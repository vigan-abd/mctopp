using System;
using System.Linq;
using System.Collections.Generic;

namespace MCTOPP.Helpers
{
    public static class ArrayExtension
    {
        public static IEnumerable<T[]> Permutations<T>(this T[] values)
        {
            if (values.Length == 1)
                return new[] { values };

            return values.SelectMany(v => Permutations(values.Except(new[] { v }).ToArray()),
                (v, p) => new[] { v }.Concat(p).ToArray());
        }

        public static IEnumerable<T[]> AllCombinations<T>(this T[] values)
        {
            var perms = values.Permutations();
            var collection = new List<T[]>();
            foreach (var item in perms)
            {
                var list = new List<T>();
                for (int i = 0; i < item.Length; i++)
                {
                    var skip = false;
                    list.Add(item[i]);

                    foreach (var other in collection)
                    {
                        if (other.Length == list.Count)
                        {
                            var same = 0;
                            for (int j = 0; j < other.Length; j++)
                            {
                                if (other[j].Equals(list[j]))
                                    same++;
                            }
                            skip = same == other.Length;
                        }
                    }

                    if (skip) continue;
                    collection.Add(list.ToArray());
                }
            }
            return collection;
        }
    }
}