using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
                var mapFilePath = args[0];
                using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
                {
                    var reader = new MapReader();
                    reader.HeroIdMapping = ReadIdMap("heroes.txt");
                    reader.SpellIdMapping = ReadIdMap("spells.txt");
                    reader.ArtifactIdMapping = ReadIdMap("artifacts.txt");
                    var mapHeader = reader.Read(new CountingStream(mapFile));

                    var output = Path.ChangeExtension(mapFilePath, ".json");
                    var json = JsonConvert.SerializeObject(mapHeader, Formatting.Indented, new JsonSerializerSettings
                    {
                        Converters = {new StringEnumConverter()}
                    });
                    File.WriteAllText(output, json);
                    Console.WriteLine("Successfully processed.");
                    Console.WriteLine("Output saved as " + output);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ERROR");
                Console.WriteLine(e);
            }
            Console.Write("Press any key to close");
            Console.ReadKey(true);
            return 0;
        }

        private static IDictionary<int,string> ReadIdMap(string mapFile)
        {
            var map = new Dictionary<int, string>();
            if (!File.Exists(mapFile))
            {
                Console.WriteLine("ID mapping file " + mapFile + " doesn't exist. Skipping.");
                
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