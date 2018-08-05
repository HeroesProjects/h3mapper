using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using H3Mapper.Flags;
using H3Mapper.Internal;
using Serilog;

namespace H3Mapper
{
    public class TemplateFileParser
    {
        private readonly Regex animationFilePattern = new Regex(@"[a-zA-Z0-9_]{3,8}\.def", RegexOptions.IgnoreCase);

        public IEnumerable<MapObjectTemplate> Parse(string path)
        {
            Log.Debug("Reading mapping file {file}", path);
            var lines = File.ReadLines(path);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("//")) continue;
                var splitLine = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (splitLine.Length != 9) throw new Exception($"Invalid line \'{line}\' in {path}");
// ah01_e.def 101111111111111111111111111111111111111111111111 010000000000000000000000000000000000000000000000 011111111 011111111 34 1 3 0


                yield return new MapObjectTemplate
                {
                    AnimationFile = ReadAnimationFile(splitLine[0]),
                    BlockPosition = ReadMask(splitLine[1]),
                    VisitPosition = ReadMask(splitLine[2]),
                    SupportedTerrainTypes = ReadFlags<Terrains>(splitLine[3]),
                    EditorMenuLocation = ReadFlags<TerrainMenus>(splitLine[4]),
                    Id = ReadEnum<ObjectId>(splitLine[5]),
                    SubId = int.Parse(splitLine[6]),
                    Type = ReadEnum<ObjectType>(splitLine[7]),
                    IsBackground = ReadBool(splitLine[8].Single())
                };
            }
        }

        private static TEnum ReadEnum<TEnum>(string rawValue) where TEnum : struct
        {
            return EnumValues.Cast<TEnum>(int.Parse(rawValue));
        }

        private TEnum ReadFlags<TEnum>(string rawValue) where TEnum : struct
        {
            var rawFlag = ReadFlag(rawValue);
            return EnumValues.Cast<TEnum>(rawFlag);
        }

        private int ReadFlag(string rawValue)
        {
            return Convert.ToInt32(rawValue, 2);
        }

        private Position ReadMask(string maskRaw)
        {
            if (maskRaw.Length != 6 * 8)
            {
                throw new ArgumentOutOfRangeException(nameof(maskRaw), $"Unexpected mask length: {maskRaw.Length}");
            }

            var positions = new bool[8, 6];
            for (int i = 0; i < maskRaw.Length; i++)
            {
                var index0 = 7 - (i % 8);
                var index1 = 5 - (i / 8);
                positions[index0, index1] = ReadBool(maskRaw[i]) == false;
            }

            return new Position {Positions = positions};
        }

        private bool ReadBool(char value)
        {
            if (value == '1')
            {
                return true;
            }

            if (value == '0')
            {
                return false;
            }

            throw new ArgumentOutOfRangeException(nameof(value), value, "Expected value to be either 1 or 0");
        }

        private string ReadAnimationFile(string file)
        {
            if (animationFilePattern.IsMatch(file))
            {
                return file;
            }

            throw new ArgumentException($"Unexpected animation file name: {file}");
        }
    }
}