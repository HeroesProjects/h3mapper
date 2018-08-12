using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace H3Mapper.Internal
{
    public class Lookup<TKey, TValue> : ILookup<TKey, TValue>
    {
        private readonly IDictionary<TKey, IList<TValue>> data;

        public Lookup(IEqualityComparer<TKey> comparer = null)
        {
            data = new Dictionary<TKey, IList<TValue>>(comparer);
        }

        public void Add(TKey key, TValue value)
        {
            if (data.TryGetValue(key, out var list) == false)
            {
                data[key] = list = new List<TValue>(3);
            }

            list.Add(value);
        }

        public IEnumerator<IGrouping<TKey, TValue>> GetEnumerator()
        {
            foreach (var pair in data)
            {
                yield return new Grouping(pair);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool Contains(TKey key) => data.ContainsKey(key);

        public int Count => data.Count;

        public IEnumerable<TValue> this[TKey key] => data[key];

        private class Grouping : IGrouping<TKey, TValue>
        {
            private readonly KeyValuePair<TKey, IList<TValue>> data;

            public Grouping(KeyValuePair<TKey, IList<TValue>> data) => this.data = data;

            public IEnumerator<TValue> GetEnumerator() => data.Value.GetEnumerator();

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public TKey Key => data.Key;
        }
    }
}