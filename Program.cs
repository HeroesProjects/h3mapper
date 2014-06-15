using System;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace H3Mapper
{
    class Program
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

        private static void ShowHelp()
        {
            Console.WriteLine("No map file specified. Call as " + Assembly.GetEntryAssembly().GetName().Name +
                              " map.h3m");
        }
    }
}