using AutoStreamDeck.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AutoStreamDeck.Events.SendEvents
{
    internal abstract class SendEvent
    {

    }

    internal abstract class SendEvent<TPayload> : SendEvent
    {

        [JsonPropertyName("event")]
        public abstract string EventName { get; }

        [JsonPropertyName("context")]
        public string Context { get; }

        [JsonPropertyName("payload")]
        public TPayload Payload { get; }

        protected SendEvent(ISDAction action, TPayload payload)
        {
            Context = action.Context;
            Payload = payload;
        }

    }
}
