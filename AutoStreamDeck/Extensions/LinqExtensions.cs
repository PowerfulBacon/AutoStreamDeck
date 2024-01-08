using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Linq
{

	internal static class LinqExtensions
	{

		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (T thing in source)
				action(thing);
			return source;
		}

		public static async Task<IEnumerable<T>> ForEach<T>(this IEnumerable<T> source, Func<T, Task> action)
		{
			foreach (T thing in source)
				await action(thing);
			return source;
		}

		public static IEnumerable<T> With<T>(this IEnumerable<T> source, T value)
		{
			yield return value;
			foreach (var thing in source)
				yield return thing;
		}

	}
}
