using StreamDeckNet.Enums;
using StreamDeckNet.Events;
using StreamDeckNet.Events.ReceiveEvents;
using StreamDeckNet.Events.SendEvents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckNet.Actions
{
	public interface ISDAction
	{
		/// <summary>
		/// This will be assigned when we create it via reflection.
		/// This saves the end-coder having to implement a constructor for every action
		/// </summary>
		string Context { get; internal set; }
	}

	/// <summary>
	/// A streamdeck aciton that has no settings associated with it.
	/// </summary>
	public abstract class SDAction : SDAction<NoSettings>
	{
	}

	public abstract class SDAction<TSettings> : ISDAction
	{

		/// <summary>
		/// This will be assigned when we create it via reflection.
		/// This saves the end-coder having to implement a constructor for every action
		/// </summary>
		public string Context { get; set; } = null!;

		public async Task SetTitle(string title, Target target = Target.BOTH, int state = 0)
		{
			await StreamDeck.DispatchEvent(new SetTitleEvent(this, new SetTitlePayload(title, target, state)));
		}

		public virtual Task OnKeyDown(string context, KeyDownPayload<TSettings> keyDownEvent) => Task.CompletedTask;

	}
}
