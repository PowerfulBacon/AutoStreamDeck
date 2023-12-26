using AutoStreamDeck.Actions;
using AutoStreamDeck.Events.ReceiveEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExamplePlugin.Actions
{
	[ActionMeta("Timer", Description = "Updates the button with a time indicating how long has passed.")]
	internal class TimerAction : SDAction
	{

		private TimeSpan soFar;

		private DateTime? startTime;

		private TaskCompletionSource pauseGate = new TaskCompletionSource();

		private bool paused = false;

		public override async Task OnKeyDown(string context, KeyDownPayload<NoSettings> keyDownEvent)
		{
			if (startTime == null)
			{
				startTime = DateTime.Now;
				_ = Task.Run(async () =>
				{
					while (true)
					{
						if (paused)
						{
							soFar += (DateTime.Now - startTime).Value;
							await pauseGate.Task;
							startTime = DateTime.Now;
							continue;
						}
						await SetTitle((soFar + (DateTime.Now - startTime))?.ToString(@"hh\:mm\:ss") ?? "X");
						await Task.Delay(1000);
					}
				});
			}
			else
			{
				paused = !paused;
				if (!paused)
				{
					pauseGate.SetResult();
					pauseGate = new TaskCompletionSource();
				}
			}
		}
	}
}
