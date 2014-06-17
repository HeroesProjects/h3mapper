using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace H3Mapper
{
    internal class Program
    {
        private static int Main(string[] args)
        {
            if (args.Length != 1)
            {
                ShowHelp();
                return -1;
            }
            try
            {
                ConfigureLogging();

                var path = args[0];
                var mappings = ConfigureMappings();
                Run(path, mappings);
            }
            catch (Exception e)
            {
                Log.Error(e, "Error processing the file(s)");
            }
            Console.Write("Press any key to close");
            Console.ReadKey(true);
            return 0;
        }

        private static void Run(string path, IDMappings mappings)
        {
            if (File.Exists(path))
            {
                Process(mappings, path);
            }
            else
            {
                if (Directory.Exists(path))
                {
                    foreach (var file in Directory.EnumerateFiles(path, "*.h3m"))
                    {
                        Process(mappings, file);
                    }
                }
                else
                {
                    Log.Information("Given path: '{path}' does not point to an existing file or directory", path);
                }
            }
        }

        private static IDMappings ConfigureMappings()
        {
            var mappings = new IDMappings
            {
                Heroes = ReadIdMap("heroes.txt"),
                Spells = ReadIdMap("spells.txt"),
                Artifacts = ReadIdMap("artifacts.txt")
            };
            return mappings;
        }

        private static void ConfigureLogging()
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.ColoredConsole()
                .MinimumLevel.Debug()
                .CreateLogger();
        }

        private static void Process(IDMappings idMappings, string mapFilePath)
        {
            Log.Debug("Processing {file}", mapFilePath);
            using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
            {
                var reader = new MapReader();
                reader.HeroIdMapping = idMappings.Heroes;
                reader.SpellIdMapping = idMappings.Spells;
                reader.ArtifactIdMapping = idMappings.Artifacts;
                var mapHeader = reader.Read(new MapDeserializer(new CountingStream(mapFile)));

                var output = Path.ChangeExtension(mapFilePath, ".json");
                var json = JsonConvert.SerializeObject(mapHeader, Formatting.Indented,
                    new JsonSerializerSettings
                    {
                        Converters = {new StringEnumConverter()}
                    });
                File.WriteAllText(output, json);
                Console.WriteLine("Successfully processed.");
                Console.WriteLine("Output saved as " + output);
            }
        }

        private static IDictionary<int, string> ReadIdMap(string mapFile)
        {
            var map = new Dictionary<int, string>();
            if (!File.Exists(mapFile))
            {
                Log.Information("ID mapping file {file} doesn't exist. Skipping.", mapFile);
                return map;
            }
            var lines = File.ReadAllLines(mapFile);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var splitLine = line.Split(':');
                if (splitLine.Length != 2)
                {
                    throw new Exception("Invalid line '" + line + "' in " + mapFile);
                }
                var idRaw = splitLine[0];
                var name = splitLine[1];
                int id;
                if (int.TryParse(idRaw.Trim(), out id) == false)
                {
                    throw new Exception("Invalid line '" + line + "' in " + mapFile + ". " + idRaw +
                                        " is not a recognizable number.");
                }
                map[id] = name.Trim();
            }
            return map;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("No map file specified. Call as " + Assembly.GetEntryAssembly().GetName().Name +
                              " map.h3m");
        }
    }
}