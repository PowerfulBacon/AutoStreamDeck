using AutoStreamDeck.Actions;
using AutoStreamDeck.Events.ReceiveEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace ExamplePlugin.Actions
{
	[ActionMeta("Random Number", Description = "Generates a random number and sets the title of the button to the number.")]
	internal class RandomNumberAction : SDAction<RandomNumberSettings>
	{

		public override async Task OnKeyDown(string context,KeyDownPayload<RandomNumberSettings> keyDownEvent)
		{
			Random random = new Random();
			await SetTitle(random.Next(keyDownEvent.Settings.Minimum, keyDownEvent.Settings.Maximum + 1).ToString());
		}

	}

	internal class RandomNumberSettings
	{
		public int Minimum = 1;
		public int Maximum = 6;
	}

	[ActionMeta("Fixed Random Number", Description = "Generates a random number and sets the title of the button to the number.")]
	internal class RandomNumberFixedAction : SDAction
	{

		public override async Task OnKeyDown(string context, KeyDownPayload<NoSettings> keyDownEvent)
		{
			Random random = new Random();
			await SetTitle(random.Next(1, 7).ToString());
		}

	}
}
