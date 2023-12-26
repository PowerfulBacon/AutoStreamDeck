using AutoStreamDeck.Enums;
using AutoStreamDeck.Events;
using AutoStreamDeck.Events.ReceiveEvents;
using AutoStreamDeck.Events.SendEvents;
using AutoStreamDeck.Objects;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStreamDeck.Actions
{
	public interface ISDAction
	{
		/// <summary>
		/// This will be assigned when we create it via reflection.
		/// This saves the end-coder having to implement a constructor for every action
		/// </summary>
#if NET8_0
		string Context { get; internal set; }
#else
		string Context { get; set; }
#endif
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
#if NET8_0
		public string Context { get; set; } = null!;
#else
		public string Context { get; set; } = null;
#endif

		public async Task SetTitle(string title, Target target = Target.BOTH, int state = 0)
		{
			await StreamDeck.DispatchEvent(new SetTitleEvent(this, new SetTitlePayload(title, target, state)));
		}

		public async Task SetImage(Bitmap bitmapImage, Target target = Target.BOTH, int state = 0)
		{
			using (MemoryStream m = new MemoryStream())
			{
				bitmapImage.Save(m, ImageFormat.Png);
				await StreamDeck.DispatchEvent(new SetImageEvent(this, new SetImagePayload($"data:image/png;base64,{Convert.ToBase64String(m.ToArray(), Base64FormattingOptions.None)}", target, state)));
			}
		}

		public virtual Task OnKeyDown(string context, KeyDownPayload<TSettings> keyDownEvent) => Task.CompletedTask;

	}
}
