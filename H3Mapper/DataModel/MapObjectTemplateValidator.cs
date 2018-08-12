using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using H3Mapper.Flags;
using Serilog;

namespace H3Mapper.DataModel
{
    public class MapObjectTemplateValidator
    {
        private readonly IdMappings maps;

        public MapObjectTemplateValidator(IdMappings maps)
        {
            this.maps = maps;
        }

        public void Validate(MapObjectTemplate template)
        {
            var templates = maps.GetTemplatesMatching(template);
            if (templates.Length == 0)
            {
                Log.Warning("Map object template {template} is unknown.",
                    FormatObject(template));
            }

//            var match = templates.SingleOrDefault(t => MapObjectTemplateComparer.Equals(t, template));
//            if (match != null) return;
//
//            // There can be more than one this time
//            // Ignoring this has no real effect on gameplay and some 
//            match = templates.FirstOrDefault(t => MapObjectTemplateComparer.EqualsTighteningTerrain(t, template));
//            if (match != null) return;
//            Log.Information("Map object template {template} doesn't have an exact match. Candidates are {templates}",
//                FormatObject(template), templates.Select(FormatObject).ToArray());
        }

        private string Format(MapObjectTemplate template)
        {
            return $"{nameof(template.AnimationFile)}: {template.AnimationFile}, " +
                   $"{nameof(template.Id)}: {template.Id}, " +
                   $"{nameof(template.SubId)}: {template.SubId}, " +
                   $"{nameof(template.Type)}: {template.Type}";
        }

        private string FormatObject(MapObjectTemplate template)
        {
// AVCtowx0.def 000001110000011110001111111111111111111111111111 001000000000000000000000000000000000000000000000 011111111 011111111 98 2 1 0

            return $"{template.AnimationFile} " +
                   $"{FormatObjectMask(template.BlockPosition)} " +
                   $"{FormatObjectMask(template.VisitPosition)} " +
                   $"{FormatObjectFlag((int) template.SupportedTerrainTypes)} " +
                   $"{FormatObjectFlag((int) template.EditorMenuLocation)} " +
                   $"{(int) template.Id} " +
                   $"{template.SubId} " +
                   $"{(int) template.Type} " +
                   $"{Convert.ToInt32(template.IsBackground)}";
        }

        private string FormatObjectMask(Position position)
        {
            var sb = new StringBuilder(6 * 8);
            for (var i = 0; i < 6 * 8; i++)
            {
                var index0 = 5 - (i / 8);
                var index1 = 7 - (i % 8);
                sb.Append(Convert.ToInt32(position.Positions[index0, index1] == false));
            }

            return sb.ToString();
        }

        private string FormatObjectFlag(int flagValue)
        {
            return Convert.ToString(flagValue, 2).PadLeft(9, '0');
        }


        private sealed class MapObjectTemplateEqualityComparer : IEqualityComparer<MapObjectTemplate>
        {
            public bool Equals(MapObjectTemplate x, MapObjectTemplate y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.AnimationFile, y.AnimationFile, StringComparison.InvariantCultureIgnoreCase) &&
                       x.SupportedTerrainTypes == y.SupportedTerrainTypes &&
                       x.Id == y.Id &&
                       x.SubId == y.SubId &&
                       x.Type == y.Type &&
                       x.EditorMenuLocation == y.EditorMenuLocation &&
                       x.BlockPosition.ToString().Equals(y.BlockPosition.ToString()) &&
                       x.VisitPosition.ToString().Equals(y.VisitPosition.ToString()) &&
                       x.IsBackground == y.IsBackground;
            }

            public int GetHashCode(MapObjectTemplate obj)
            {
                unchecked
                {
                    var hashCode = StringComparer.InvariantCultureIgnoreCase.GetHashCode(obj.AnimationFile);
                    hashCode = (hashCode * 397) ^ (int) obj.SupportedTerrainTypes;
                    hashCode = (hashCode * 397) ^ (int) obj.Id;
                    hashCode = (hashCode * 397) ^ obj.SubId;
                    hashCode = (hashCode * 397) ^ (int) obj.Type;
                    hashCode = (hashCode * 397) ^ (int) obj.EditorMenuLocation;
                    hashCode = (hashCode * 397) ^ obj.BlockPosition.ToString().GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.VisitPosition.ToString().GetHashCode();
                    hashCode = (hashCode * 397) ^ obj.IsBackground.GetHashCode();
                    return hashCode;
                }
            }

            public bool EqualsTighteningTerrain(MapObjectTemplate x, MapObjectTemplate y)
            {
                if (ReferenceEquals(x, y)) return true;
                if (ReferenceEquals(x, null)) return false;
                if (ReferenceEquals(y, null)) return false;
                if (x.GetType() != y.GetType()) return false;
                return string.Equals(x.AnimationFile, y.AnimationFile, StringComparison.InvariantCultureIgnoreCase) &&
                       // Does Y have at least the same flags as X set. Can have more
                       (x.SupportedTerrainTypes & y.SupportedTerrainTypes) != 0 &&
                       x.Id == y.Id &&
                       x.SubId == y.SubId &&
                       x.Type == y.Type &&
                       x.EditorMenuLocation == y.EditorMenuLocation &&
                       x.BlockPosition.ToString().Equals(y.BlockPosition.ToString()) &&
                       x.VisitPosition.ToString().Equals(y.VisitPosition.ToString()) &&
                       x.IsBackground == y.IsBackground;
            }
        }

        private static MapObjectTemplateEqualityComparer MapObjectTemplateComparer { get; } =
            new MapObjectTemplateEqualityComparer();
    }
}