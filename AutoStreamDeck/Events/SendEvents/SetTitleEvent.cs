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
        /// <summary>
        /// Title to display; when no title is specified, the title will reset to the title set by the user.
        /// </summary>
        public string title { get; }
        /// <summary>
        /// Specifies which aspects of the Stream Deck should be updated, hardware, software, or both.
        /// </summary>
        public Target target { get; }
        /// <summary>
        /// Action state the request applies to; when no state is supplied, the title is set for both states. Note, only applies to multi-state actions.
        /// </summary>
        public int state { get; }

        public SetTitlePayload(string title, Target target, int state)
        {
            this.title = title;
            this.target = target;
            this.state = state;
        }
    }
}
