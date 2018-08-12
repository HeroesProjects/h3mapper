using System.Collections.Generic;
using System.Linq;
using H3Mapper.Flags;

namespace H3Mapper.DataModel
{
    public class IdMap
    {
        private readonly IDictionary<int, string> @default;

        private readonly IDictionary<MapFormat, IDictionary<int, string>> specific =
            new Dictionary<MapFormat, IDictionary<int, string>>();

        private IDictionary<int, string>[] valueChain;

        public IdMap(IDictionary<int, string> @default)
        {
            this.@default = @default;
        }

        public bool IsEmpty
        {
            get
            {
                if (@default.Count > 0)
                {
                    return false;
                }

                return specific.Any(s => s.Value.Count > 0) == false;
            }
        }

        public void AddFormatMapping(MapFormat format, IDictionary<int, string> mapping)
        {
            specific.Add(format, mapping);
        }

        public void SetCurrentSpecific(MapFormat format)
        {
            var chain = new List<IDictionary<int, string>>(specific.Count + 1);
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

        public bool TryGetValue(int id, out string value)
        {
            foreach (var collection in valueChain)
            {
                if (collection.TryGetValue(id, out value))
                {
                    return true;
                }
            }

            value = null;
            return false;
        }
    }
}