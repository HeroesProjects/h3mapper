using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Reflection;
using H3Mapper.Flags;
using H3Mapper.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace H3Mapper
{
    internal class Program
    {
        private static string RootFolder = null;
        private static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowHelp();
                return -1;
            }

            RootFolder = Path.GetDirectoryName(Environment.GetCommandLineArgs()[0]);
            var result = 0;
            try
            {
                ConfigureLogging(args.Contains("-d"));

                var path = args[0];

                if (args.Contains("-u"))
                {
                    Unpack(path);
                }
                else
                {
                    var mappings = ConfigureMappings();
                    result = Run(path, mappings, args.Contains("-s"));
                }
            }
            catch (Exception e)
            {
                Log.Error(e, "Error processing the file(s)");
                result = e.HResult;
            }

            Console.Write("Press any key to close");
            Console.ReadKey(true);
            return result;
        }

        private static void Unpack(string mapFilePath)
        {
            using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
            {
                using (var memoryStream = new MemoryStream())
                {
                    mapFile.CopyTo(memoryStream);
                    File.WriteAllBytes(Path.ChangeExtension(mapFilePath, ".decompressed"), memoryStream.ToArray());
                }
            }
        }

        private static int Run(string path, IdMappings mappings, bool skipOutput)
        {
            if (File.Exists(path))
            {
                return Process(mappings, path, skipOutput);
            }

            if (Directory.Exists(path))
            {
                var result = 0;
                foreach (var file in Directory.EnumerateFiles(path, "*.h3m"))
                {
                    var fileResult = Process(mappings, file, skipOutput);
                    if (fileResult != 0)
                    {
                        result = fileResult;
                    }
                }

                return result;
            }

            Log.Information("Given path: '{path}' does not point to an existing file or directory", path);
            return 1;
        }

        private static IdMappings ConfigureMappings()
        {
            return new IdMappings(
                ReadIdMap("heroes.txt"),
                ReadIdMap("spells.txt"),
                ReadIdMap("artifacts.txt"),
                ReadIdMap("monsters.txt"));
        }

        private static void ConfigureLogging(bool forceDebug)
        {
            var configuration = new LoggerConfiguration()
                .WriteTo.File("logs.log")
                .WriteTo.Console(theme: new AnsiConsoleTheme(new Dictionary<ConsoleThemeStyle, string>
                {
                    [ConsoleThemeStyle.Text] = "\x001B[38;5;0253m",
                    [ConsoleThemeStyle.SecondaryText] = "\x001B[38;5;0246m",
                    [ConsoleThemeStyle.TertiaryText] = "\x001B[38;5;0253m",
                    [ConsoleThemeStyle.Invalid] = "\x001B[33;1m",
                    [ConsoleThemeStyle.Null] = "\x001B[38;5;0038m",
                    [ConsoleThemeStyle.Name] = "\x001B[38;5;0081m",
                    [ConsoleThemeStyle.String] = "\x001B[38;5;0216m",
                    [ConsoleThemeStyle.Number] = "\x001B[38;5;151m",
                    [ConsoleThemeStyle.Boolean] = "\x001B[38;5;0038m",
                    [ConsoleThemeStyle.Scalar] = "\x001B[38;5;0079m",
                    [ConsoleThemeStyle.LevelVerbose] = "\x001B[37m",
                    [ConsoleThemeStyle.LevelDebug] = "\x001B[37m",
                    [ConsoleThemeStyle.LevelInformation] = "\x001B[37;1m",
                    [ConsoleThemeStyle.LevelWarning] = "\x001B[38;5;0229m",
                    [ConsoleThemeStyle.LevelError] = "\x001B[38;5;0197m\x001B[48;5;0238m",
                    [ConsoleThemeStyle.LevelFatal] = "\x001B[38;5;0197m\x001B[48;5;0238m"
                }));
#if DEBUG
            forceDebug = true;
#endif
            if (forceDebug)
            {
                configuration.MinimumLevel.Debug();
            }

            Log.Logger = configuration.CreateLogger();
        }

        private static int Process(IdMappings idMappings, string mapFilePath, bool skipOutput)
        {
            Log.Debug("Processing {file}", mapFilePath);
            using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
            {
                var reader = new MapReader(idMappings);
                H3Map mapHeader;
                try
                {
                    mapHeader = reader.Read(new MapDeserializer(new PositionTrackingStream(mapFile)));
                }
                catch (InvalidDataException e)
                {
                    Log.Error(e, "Failed to process map {file}. File is most likely corrupted.", mapFilePath);
                    return e.HResult;
                }

                Console.WriteLine("Successfully processed.");
                if (skipOutput)
                {
                    Log.Debug("Skipping writing output file.");
                    return 0;
                }

                var output = Path.ChangeExtension(mapFilePath, ".json");
                var json = JsonConvert.SerializeObject(mapHeader, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        Converters = {new StringEnumConverter()}
                    });
                File.WriteAllText(output, json);
                Console.WriteLine($"Output saved as {output}");
                return 0;
            }
        }

        private static IdMappings.IdMap ReadIdMap(string mapFileName)
        {
            var mapFileLocation = Path.Combine(RootFolder, "data", mapFileName);
            var @default = new Dictionary<int, string>();
            var map = new IdMappings.IdMap(@default);
            if (!File.Exists(mapFileLocation))
            {
                Log.Information("ID mapping file {file} doesn't exist. Skipping.", mapFileLocation);
                return map;
            }

            foreach (var format in new[] {MapFormat.RoE, MapFormat.AB, MapFormat.SoD, MapFormat.HotA, MapFormat.WoG})
            {
                var file = Path.ChangeExtension(mapFileLocation, $"{format}.txt");
                if (File.Exists(file))
                {
                    var values = new Dictionary<int, string>();
                    ReadFileValues(file, values);
                    map.AddFormatMapping(format, values);
                }
                else
                {
                    Log.Debug("ID mapping file {file} doesn't exist. Skipping.", file);
                }
            }

            ReadFileValues(mapFileLocation, @default);
            return map;
        }

        private static void ReadFileValues(string mapFile, Dictionary<int, string> map)
        {
            var parser = new DataFileParser();
            foreach (var idToValue in parser.Parse(mapFile))
            {
                try
                {
                    map.Add(idToValue.Key, idToValue.Value);
                }
                catch (ArgumentException e)
                {
                    throw new Exception(
                        $"Invalid item \'{idToValue}\' in {mapFile}. Item with the same Id already exists.",
                        e);
                }
            }
        }

        private static void ShowHelp()
        {
            Console.WriteLine($"No map file specified. Call as {Assembly.GetEntryAssembly().GetName().Name} map.h3m");
        }
    }
}