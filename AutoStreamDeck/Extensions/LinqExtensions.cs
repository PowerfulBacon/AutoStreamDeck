using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{

	internal static class LinqExtensions
	{

		public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (T thing in source)
				action(thing);
		}

		public static async Task ForEach<T>(this IEnumerable<T> source, Func<T, Task> action)
		{
			foreach (T thing in source)
				await action(thing);
		}

	}
}
