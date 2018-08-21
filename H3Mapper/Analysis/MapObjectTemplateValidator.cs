using System;
using System.Linq;
using System.Text;
using H3Mapper.DataModel;
using H3Mapper.MapModel;
using Serilog;

namespace H3Mapper.Analysis
{
    public class MapObjectTemplateValidator : IMapValidator
    {
        private readonly IdMappings maps;

        public MapObjectTemplateValidator(IdMappings maps)
        {
            this.maps = maps;
        }

        public void Validate(H3Map map)
        {
            foreach (var template in map.Objects.Select(x => x.Template).Distinct())
            {
                ValidateTemplate(template);
            }
        }

        private void ValidateTemplate(MapObjectTemplate template)
        {
            var templates = maps.GetTemplatesMatching(template);
            if (templates.Length == 0)
            {
                Log.Warning("Map object template {template} is unknown.",
                    FormatObject(template));
            }
        }

        private static string FormatObject(MapObjectTemplate template)
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

        private static string FormatObjectMask(Position position)
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

        private static string FormatObjectFlag(int flagValue)
        {
            return Convert.ToString(flagValue, 2).PadLeft(11, '0');
        }
    }
}