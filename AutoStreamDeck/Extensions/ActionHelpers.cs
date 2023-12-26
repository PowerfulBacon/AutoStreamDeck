using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace AutoStreamDeck.Extensions
{
	internal static class ActionHelpers
	{

		private static Regex invalidCharacterCleaner = new Regex("[^a-zA-Z]", RegexOptions.Compiled);

		public static string MakeStringPath(string value) => invalidCharacterCleaner.Replace(value, "").ToLower();

	}
}
