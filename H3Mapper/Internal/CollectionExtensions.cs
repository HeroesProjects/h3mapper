using System.Collections.Generic;

namespace H3Mapper.Internal
{
    public static class CollectionExtensions
    {
        public static bool TryAdd<T>(this ICollection<T> collection, T item) where T : class
        {
            if (item != null)
            {
                collection.Add(item);
                return true;
            }
            return false;
        }
    }
}