using AutoStreamDeck.Actions;
using AutoStreamDeck.Events.ReceiveEvents;
using AutoStreamDeck.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AutoStreamDeck.Objects
{
	internal class ContextualEvent
	{

		/// <summary>
		/// Reflected action types
		/// </summary>
		internal static Dictionary<string, Type> EventTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(x => x.GetCustomAttribute<ReceiveEventNameAttribute>() != null)
			.ToDictionary(x => x.GetCustomAttribute<ReceiveEventNameAttribute>()!.EventName, x => x);

		internal static Dictionary<string, string> EventMethodNames = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(x => x.GetCustomAttribute<ReceiveEventNameAttribute>() != null)
			.ToDictionary(x => x.GetCustomAttribute<ReceiveEventNameAttribute>()!.EventName, x => x.GetCustomAttribute<ReceiveEventFunctionNameAttribute>()!.FunctionName);

		private ContextualAction parent;

		/// <summary>
		/// The type of the payload. This is the full type, including the resolved generic parameter
		/// </summary>
		private Type payloadType;

		private ReceiveEvent LocalEvent;

		private MethodInfo linkedMethod;

		public ContextualEvent(ContextualAction parent, string eventName)
		{
			this.parent = parent;
			// Create the local events
			LocalEvent = (ReceiveEvent)Activator.CreateInstance(EventTypes[eventName].MakeGenericType(parent.SettingsType))!;
			payloadType = LocalEvent.GetType().BaseType!.GetGenericArguments()[0];
			linkedMethod = parent.AssignedAction.GetType().GetMethod(EventMethodNames[eventName], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				?? throw new NullReferenceException($"Could not locate the method {EventMethodNames[eventName]} on the type {LocalEvent.GetType().Name}");
		}

		public async Task Raise(JsonNode payload)
		{
			// Convert the payload into the correct payload type
			object? serialisedPayload = JsonSerializer.Deserialize(payload, payloadType);
			if (serialisedPayload == null)
				throw new NullReferenceException($"Could not deserialise the payload {payload.ToJsonString()} to {payloadType.Name}.");
			StreamDeck.LogMessage($"Attempting to invoke method {payloadType.Name}...");
			// Invoke the method
			Task result = (Task)linkedMethod.Invoke(parent.AssignedAction, new object[] { parent.ContextID, serialisedPayload })!;
			await result;
			StreamDeck.LogMessage($"Successfully executed payload of type {payloadType.Name}.");
		}

	}
}
