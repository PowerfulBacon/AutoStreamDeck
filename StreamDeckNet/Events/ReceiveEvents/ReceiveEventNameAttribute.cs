using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamDeckNet.Events.ReceiveEvents
{
	[AttributeUsage(AttributeTargets.Class)]
	internal class ReceiveEventNameAttribute : Attribute
	{

		public string EventName { get; set; }

		public ReceiveEventNameAttribute(string eventName)
		{
			EventName = eventName;
		}
	}
}
