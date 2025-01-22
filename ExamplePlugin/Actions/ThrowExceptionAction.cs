using AutoStreamDeck.Actions;
using AutoStreamDeck.Events.ReceiveEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamplePlugin.Actions
{
    [ActionMeta("Throw Exception", Description = "Throws a not implemented exception when pressed.")]
    class ThrowExceptionAction : SDAction
    {

        public override Task OnKeyDown(string context, KeyDownPayload<NoSettings> keyDownEvent)
        {
            throw new NotImplementedException();
        }

    }
}
