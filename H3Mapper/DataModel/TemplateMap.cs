using System;
using System.Collections.Generic;
using System.Linq;
using H3Mapper.Flags;

namespace H3Mapper.DataModel
{
    public class TemplateMap
    {
        private readonly ILookup<String, MapObjectTemplate> @default;
        private ILookup<String, MapObjectTemplate>[] valueChain;

        private readonly IDictionary<MapFormat, ILookup<String, MapObjectTemplate>> specific =
            new Dictionary<MapFormat, ILookup<String, MapObjectTemplate>>();

        public TemplateMap(MapObjectTemplate[] defaults)
        {
            @default = BuildMapping(defaults);
        }

        public MapObjectTemplate[] GetValues(MapObjectTemplate template)
        {
            var key = template.AnimationFile;
            foreach (var collection in valueChain)
            {
                if (collection.Contains(key))
                {
                    return collection[key].ToArray();
                }
            }

            return new MapObjectTemplate[0];
        }

        public void AddFormatMapping(MapFormat format, MapObjectTemplate[] values)
        {
            specific.Add(format, BuildMapping(values));
        }

        private ILookup<String, MapObjectTemplate> BuildMapping(MapObjectTemplate[] values)
        {
            var result = new Internal.Lookup<String, MapObjectTemplate>(StringComparer.OrdinalIgnoreCase);
            foreach (var template in values)
            {
                result.Add(template.AnimationFile, template);
            }

            return result;
        }

        public void SetCurrentSpecific(MapFormat format)
        {
            var chain = new List<ILookup<String, MapObjectTemplate>>(specific.Count + 1);
            var currentFormat = format;
            do
            {
                if (specific.TryGetValue(currentFormat, out var currentValue))
                {
                    chain.Add(currentValue);
                }

                currentFormat = GetPreviousFormatInChain(currentFormat);
            } while (currentFormat != 0);

            // always at least have the default
            chain.Add(@default);
            valueChain = chain.ToArray();
        }

        private MapFormat GetPreviousFormatInChain(MapFormat format)
        {
            switch (format)
            {
                case MapFormat.HotA:
                case MapFormat.WoG:
                    return MapFormat.SoD;
                case MapFormat.SoD:
                    return MapFormat.AB;
                case MapFormat.AB:
                    return MapFormat.RoE;
                default:
                    return 0;
            }
        }
    }
}