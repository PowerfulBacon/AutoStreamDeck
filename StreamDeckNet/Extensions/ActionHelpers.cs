using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace StreamDeckNet.Extensions
{
	internal static class ActionHelpers
	{

		private static Regex invalidCharacterCleaner = new Regex("[^a-zA-Z]", RegexOptions.Compiled);

		public static string ActionNameToURI(string actionName)
		{
			return $"com.{StreamDeck.PluginAuthor}.{StreamDeck.PluginName}.{invalidCharacterCleaner.Replace(actionName, "").ToLower()}";
		}

	}
}
