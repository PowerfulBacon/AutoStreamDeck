using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace AutoStreamDeck.Extensions
{
	internal static class ReflectionHelpers
	{

		// This needs to stay loaded.
		private static MetadataLoadContext metadataLoader;

		public static Assembly[] ReflectedAssemblies { get; private set; } = new Assembly[0];

		public static void LoadAdditionalAssemblies(Assembly[] assemblies)
		{
			if (assemblies == null)
				assemblies = new Assembly[0];
			ReflectedAssemblies = assemblies
				// Load the entry assembly
				.With(Assembly.GetEntryAssembly())
				// Load the library assembly
                .With(Assembly.GetAssembly(typeof(ReflectionHelpers)))
                .Select(x => Assembly.Load(x.GetName()))
				.ToArray();
		}

	}
}
