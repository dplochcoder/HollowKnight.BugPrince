using System;
using System.Collections.Generic;
using System.Linq;

namespace BugPrince.Scripts.Lib
{
    public static class CollectionsExt
    {
        public static V GetOrAddNew<K, V>(this Dictionary<K, V> self, K key) where V : new()
        {
            if (self.TryGetValue(key, out var value)) return value;
            else
            {
                value = new V();
                self.Add(key, value);
                return value;
            }
        }

        public static void ForEach<T>(this IEnumerable<T> self, Action<T> action)
        {
            foreach (var item in self) action(item);
        }

        public static void SortBy<T, C>(this List<T> self, Func<T, C> keyExtractor) where C : IComparable<C>
        {
            var indexed = self.Select(t => (t, keyExtractor(t))).ToList();
            indexed.Sort((p1, p2) => p1.Item2.CompareTo(p2.Item2));

            self.Clear();
            self.AddRange(indexed.Select(p => p.Item1));
        }
    }
}
