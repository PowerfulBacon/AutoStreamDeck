using StreamDeckNet.Actions;
using StreamDeckNet.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace StreamDeckNet.Events.SendEvents
{
    internal class SetTitleEvent : SendEvent<SetTitlePayload>
    {
        public SetTitleEvent(ISDAction action, SetTitlePayload payload) : base(action, payload)
        {
        }

		[JsonPropertyName("event")]
		public override string EventName => "setTitle";
    }

    internal class SetTitlePayload
    {
        public string title { get; }
        public Target target { get; }
        public int state { get; }

        public SetTitlePayload(string title, Target target, int state)
        {
            this.title = title;
            this.target = target;
            this.state = state;
        }
    }
}
