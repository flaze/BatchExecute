using System.Collections.Generic;
using System.Linq;

namespace BatchExecute
{
    public static class Utils
    {
        public static IEnumerable<int> EnumerableRange(int min, int max, int steps)
        {
            return Enumerable.Range(0, steps)
                             .Select(i => (int)(min + (max - min) * ((double)i / (steps))));
        }
    }
}
