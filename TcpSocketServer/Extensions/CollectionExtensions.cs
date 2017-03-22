using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace TcpSocketServer.Extensions
{
    public static class CollectionExtensions
    {
        /// <summary>
        /// Functional foreach loop.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="func"></param>
        [DebuggerStepThrough]
        public static IEnumerable<T> ForEach<T>(this IEnumerable<T> collection, Action<T> func)
        {
            foreach (var item in collection ?? Enumerable.Empty<T>())
            {
                func(item);
            }

            return collection;
        }
    }
}
