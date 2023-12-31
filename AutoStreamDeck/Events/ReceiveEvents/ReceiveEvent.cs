﻿using AutoStreamDeck.Actions;
using AutoStreamDeck.Events.SendEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoStreamDeck.Events.ReceiveEvents
{
	public class ReceiveEvent
	{
		[JsonPropertyName("device")]
		public string Device { get; }
	}

	public abstract class ReceiveEvent<TPayload> : ReceiveEvent
	{
		[JsonPropertyName("payload")]
		public TPayload Payload { get; }

	}
}
