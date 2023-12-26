using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoStreamDeck.Objects
{
	public class PluginInformation
	{

		public string PluginName { get; set; } = Assembly.GetExecutingAssembly().GetName().Name ?? "ExamplePlugin";

#if NET8_0
		public string? Description { get; set; } = null;
#else
		public string Description { get; set; } = null;
#endif

		public string Author { get; set; } = "DefaultAuthor";

		public string Version { get; set; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion ?? "1.0.0";

	}
}
