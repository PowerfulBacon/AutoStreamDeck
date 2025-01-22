using AutoStreamDeck.Actions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace AutoStreamDeck.Events.SendEvents
{
    internal class ShowAlertEvent : SendEvent<NoSettings>
    {
        public ShowAlertEvent(ISDAction action, NoSettings payload) : base(action, payload)
        {
        }

        [JsonPropertyName("event")]
        public override string EventName => "showAlert";
    }
}
