using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoStreamDeck.Events.ReceiveEvents
{
	/// <summary>
	/// What's the name of the function that we map to?
	/// </summary>
	[AttributeUsage(AttributeTargets.Class)]
	internal class ReceiveEventFunctionNameAttribute : Attribute
	{

		public string FunctionName { get; }

		public ReceiveEventFunctionNameAttribute(string functionName)
		{
			FunctionName = functionName;
		}
	}
}
