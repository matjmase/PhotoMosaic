using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotoMosaic.Common.Extensions
{
    public static class CollectionExtensions
    {
        public static T MaxOrDefault<T>(this IEnumerable<T> collection, Func<T, double> scoring)
        {
            return collection.FindOneOrDefault(scoring, (first, second) => first > second);
        }
        public static T MinOrDefault<T>(this IEnumerable<T> collection, Func<T, double> scoring)
        {
            return collection.FindOneOrDefault(scoring, (first, second) => first < second);
        }

        private static T FindOneOrDefault<T>(this IEnumerable<T> collection, Func<T, double> scoring, Func<double, double, bool> compare)
        {
            var output = default(T);
            var first = true;

            foreach (var element in collection)
            {
                if (first)
                {
                    output = element;
                    first = false;
                }
                else if (compare(scoring(element), scoring(output)))
                {
                    output = element;
                }
            }

            return output;
        }
    }
}
