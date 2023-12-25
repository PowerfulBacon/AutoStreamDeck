using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StreamDeckNet.Objects
{
	public class Coordinate
	{
		[JsonPropertyName("column")]
		public int Column { get; set; }
		[JsonPropertyName("row")]
		public int Row { get; set; }
	}
}
