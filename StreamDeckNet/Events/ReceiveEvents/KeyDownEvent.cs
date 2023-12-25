using StreamDeckNet.Objects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StreamDeckNet.Events.ReceiveEvents
{
	[ReceiveEventName("keyDown")]
	[ReceiveEventFunctionName("OnKeyDown")]
	public class KeyDownEvent<TSettings> : ReceiveEvent<KeyDownPayload<TSettings>>
	{
	}

	public class KeyDownPayload<TSettings>
	{

		[JsonPropertyName("coordinates")]
		public Coordinate Coordinates { get; set; }

		[JsonPropertyName("state")]
		public int State { get; set; }

		[JsonPropertyName("userDesiredState")]
		public int UserDesiredState { get; set; }

		[JsonPropertyName("isInMultiAction")]
		public bool IsInMultiAction { get; set; }

		[JsonPropertyName("settings")]
		public TSettings Settings { get; set; }
	}
}
