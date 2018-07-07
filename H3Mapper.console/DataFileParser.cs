using System;
using System.Collections.Generic;
using System.IO;
using Serilog;

namespace H3Mapper
{
    public class DataFileParser
    {
        public IEnumerable<KeyValuePair<int, string>> Parse(string mapFilePath)
        {
            Log.Debug("Reading mapping file {file}", mapFilePath);
            var lines = File.ReadLines(mapFilePath);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var splitLine = line.Split(':');
                if (splitLine.Length != 2) throw new Exception($"Invalid line \'{line}\' in {mapFilePath}");

                var idRaw = splitLine[0];
                var name = splitLine[1];
                if (int.TryParse(idRaw.Trim(), out var id) == false)
                {
                    throw new Exception(
                        $"Invalid line \'{line}\' in {mapFilePath}. {idRaw} is not a recognizable number.");
                }

                yield return KeyValuePair.Create(id, name);
            }
        }
    }
}