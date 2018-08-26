using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using H3Mapper.DataModel;
using H3Mapper.Flags;
using H3Mapper.MapModel;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace H3Mapper
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            if (args.Length == 0)
            {
                ShowHelp();
                return -1;
            }

            var result = 0;
            try
            {
                ConfigureLogging(
#if DEBUG
                    true,
#else
                    args.Contains("-d"),
#endif
                    args.Contains("-q")
                );
                var executor = new Executor(
                    ConfigureMappings(Path.GetDirectoryName(Environment.GetCommandLineArgs()[0])),
                    args[0],
                    args.Contains("-v"));
                result = executor.Run();
                executor.Dispose();
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

        private static IdMappings ConfigureMappings(string rootFolder)
        {
            return new IdMappings(
                ReadIdMap(Path.Combine(rootFolder, "data", "heroes.txt")),
                ReadIdMap(Path.Combine(rootFolder, "data", "spells.txt")),
                ReadIdMap(Path.Combine(rootFolder, "data", "artifacts.txt")),
                ReadIdMap(Path.Combine(rootFolder, "data", "monsters.txt")),
                ReadIdMap(Path.Combine(rootFolder, "data", "creaturegenerators1.txt")),
                ReadIdMap(Path.Combine(rootFolder, "data", "creaturegenerators4.txt")),
                ReadTemplates(Path.Combine(rootFolder, "data", "objects.txt")));
        }

        private static TemplateMap ReadTemplates(string mapFileLocation)
        {
            var parser = new TemplateFileParser();


            var map = new TemplateMap(File.Exists(mapFileLocation)
                ? parser.Parse(mapFileLocation).ToArray()
                : new MapObjectTemplate[0]);
            foreach (var format in new[] {MapFormat.RoE, MapFormat.AB, MapFormat.SoD, MapFormat.HotA, MapFormat.WoG})
            {
                var file = Path.ChangeExtension(mapFileLocation, $"{format}.txt");
                if (File.Exists(file))
                {
                    var values = parser.Parse(file).ToArray();
                    map.AddFormatMapping(format, values);
                }
                else
                {
                    Log.Debug("ID mapping file {file} doesn't exist. Skipping.", file);
                }
            }

            return map;
        }

        private static void ConfigureLogging(bool forceDebug, bool quietMode)
        {
            var configuration = new LoggerConfiguration()
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
            if (quietMode)
            {
                configuration.MinimumLevel.Warning();
            }
            else
            {
                if (forceDebug)
                {
                    configuration.MinimumLevel.Debug();
                }
            }


            Log.Logger = configuration.CreateLogger();
        }

        private static IdMap ReadIdMap(string mapFileLocation)
        {
            var @default = new Dictionary<int, string>();
            var map = new IdMap(@default);
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