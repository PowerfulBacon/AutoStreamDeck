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
				.With(Assembly.GetEntryAssembly())
				.Select(x => Assembly.Load(x.GetName()))
				.ToArray();
			/*
			PathAssemblyResolver resolver = new PathAssemblyResolver(
				assemblies
					.With(typeof(Type).Assembly)
					.With(Assembly.GetEntryAssembly())
					.Select(x => x.Location)
				);
			metadataLoader = new MetadataLoadContext(resolver, typeof(Type).Assembly.GetName().ToString());
			ReflectedAssemblies = assemblies
				.With(Assembly.GetEntryAssembly())
				.Select(x => metadataLoader.LoadFromAssemblyPath(x.Location))
				.ToArray();
			*/
		}

	}
}
