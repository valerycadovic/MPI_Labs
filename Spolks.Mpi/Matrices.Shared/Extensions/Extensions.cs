using System.Collections.Generic;
using System.Linq;

namespace Matrices.Shared.Extensions
{
    public static class ArrayExtensions
    {
        public static IDictionary<int, TValue[]> SplitToDictionary<TValue>(this TValue[] self, int groups)
        {
            return Arrays.split(self, groups)
                .Select((n, i) => new { Index = i, Value = n.ToArray() })
                .ToDictionary(k => k.Index, v => v.Value);
        }
    }
}
