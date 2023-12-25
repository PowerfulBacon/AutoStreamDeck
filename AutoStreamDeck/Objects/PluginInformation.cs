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

		public string? Description { get; set; } = null;

		public string Author { get; set; } = "DefaultAuthor";

		public string Version { get; set; } = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion ?? "1.0.0";

	}
}
