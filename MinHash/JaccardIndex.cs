using System.Collections.Generic;
using System.Linq;

namespace MinHash
{
    public static class JaccardIndex
    {
        public static double GetJaccardIndex<T>(IEnumerable<T> first, IEnumerable<T> second)
        {
            var intersectionLength = first.Intersect(second).Count();
            var unionLength = first.Union(second).Count();

            var jaccardIndex = intersectionLength / (double) unionLength;

            return jaccardIndex;
        }
    }
}
