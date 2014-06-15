using System;
using System.IO;
using System.IO.Compression;

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
            var mapFilePath = args[0];
            using (var mapFile = new GZipStream(File.OpenRead(mapFilePath), CompressionMode.Decompress))
            {
                var reader = new MapReader();
                var mapHeader = reader.Read(new CountingStream(mapFile));
                Console.WriteLine(mapHeader);
            }
            Console.ReadKey(true);
            return 0;
        }

        private static void ShowHelp()
        {
            Console.WriteLine("No map file specified");
        }
    }
}