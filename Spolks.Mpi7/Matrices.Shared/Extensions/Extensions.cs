using System.Collections.Generic;
using System.Linq;

namespace Matrices.Shared.Extensions
{
    public static class ArrayExtensions
    {
        public static IEnumerable<IEnumerable<T>> Split<T>(this T[] self, int groups)
        {
            var division = self.Length.DivideWithRemainder(groups);
            int equalParts = division.remainder == 0 ? groups : groups - 1;

            for (int i = 0; i < equalParts; i++)
            {
                yield return self.Skip(i * division.result).Take(division.result);
            }

            if (division.remainder != 0)
            {
                yield return self.TakeLast(division.result + division.remainder);
            }
        }

        public static IDictionary<int, TValue[]> SplitToDictionary<TValue>(this TValue[] self, int groups)
        {
            return self
                .Split(groups)
                .Select((n, i) => new { Index = i, Value = n.ToArray() })
                .ToDictionary(k => k.Index, v => v.Value);
        }
    }

    public static class IntegerExtensions
    {
        public static (int result, int remainder) DivideWithRemainder(this int a, int b)
        {
            return (a / b, a % b);
        }
    }
}
