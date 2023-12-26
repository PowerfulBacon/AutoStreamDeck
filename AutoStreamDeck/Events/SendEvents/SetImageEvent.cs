using AutoStreamDeck.Actions;
using AutoStreamDeck.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoStreamDeck.Events.SendEvents
{
	internal class SetImageEvent : SendEvent<SetImagePayload>
	{
		public SetImageEvent(ISDAction action, SetImagePayload payload) : base(action, payload)
		{
		}

		[JsonPropertyName("event")]
		public override string EventName => "setImage";
	}

	internal class SetImagePayload
	{
		
		public string image { get; }
		public Target target { get; }
		public int state { get; }

		public SetImagePayload(string base64, Target target, int state)
		{
			this.image = base64;
			this.target = target;
			this.state = state;
		}
	}
}
