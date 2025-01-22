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

		private static Dictionary<string, Type> _eventTypeCache;
		private static Dictionary<string, string> _eventMethodCache;

		/// <summary>
		/// Reflected action types
		/// </summary>
		internal static Dictionary<string, Type> EventTypes
		{
			get {
				if (_eventTypeCache != null)
					return _eventTypeCache;
				_eventTypeCache = ReflectionHelpers.ReflectedAssemblies
					.SelectMany(x => x.GetTypes())
					.Where(x => x.GetCustomAttribute<ReceiveEventNameAttribute>() != null)
					.ToDictionary(x => x.GetCustomAttribute<ReceiveEventNameAttribute>().EventName, x => x);
				return _eventTypeCache;
			}
		}

		internal static Dictionary<string, string> EventMethodNames
		{
			get
			{
				if (_eventMethodCache != null)
					return _eventMethodCache;
				_eventMethodCache = ReflectionHelpers.ReflectedAssemblies
					.SelectMany(x => x.GetTypes())
					.Where(x => x.GetCustomAttribute<ReceiveEventNameAttribute>() != null)
					.ToDictionary(x => x.GetCustomAttribute<ReceiveEventNameAttribute>().EventName, x => x.GetCustomAttribute<ReceiveEventFunctionNameAttribute>().FunctionName);
				return _eventMethodCache;
			}
		}

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
#if NET8_0
			LocalEvent = (ReceiveEvent)Activator.CreateInstance(EventTypes[eventName].MakeGenericType(parent.SettingsType))!;
			payloadType = LocalEvent.GetType().BaseType!.GetGenericArguments()[0];
			linkedMethod = parent.AssignedAction.GetType().GetMethod(EventMethodNames[eventName], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				?? throw new NullReferenceException($"Could not locate the method {EventMethodNames[eventName]} on the type {LocalEvent.GetType().Name}");
#else
			LocalEvent = (ReceiveEvent)Activator.CreateInstance(EventTypes[eventName].MakeGenericType(parent.SettingsType));
			payloadType = LocalEvent.GetType().BaseType.GetGenericArguments()[0];
			linkedMethod = parent.AssignedAction.GetType().GetMethod(EventMethodNames[eventName], BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
				?? throw new NullReferenceException($"Could not locate the method {EventMethodNames[eventName]} on the type {LocalEvent.GetType().Name}");
#endif
		}

		public async Task Raise(JsonNode payload)
		{
			// Convert the payload into the correct payload type
#if NET8_0
			object? serialisedPayload = JsonSerializer.Deserialize(payload, payloadType);
#else
			object serialisedPayload = JsonSerializer.Deserialize(payload, payloadType);
#endif
			if (serialisedPayload == null)
				throw new NullReferenceException($"Could not deserialise the payload {payload.ToJsonString()} to {payloadType.Name}.");
			StreamDeck.LogMessage($"Attempting to invoke method {payloadType.Name}...");
			// Invoke the method
			try
			{
#if NET8_0
			Task result = (Task)linkedMethod.Invoke(parent.AssignedAction, new object[] { parent.ContextID, serialisedPayload })!;
#else
				Task result = (Task)linkedMethod.Invoke(parent.AssignedAction, new object[] { parent.ContextID, serialisedPayload });
#endif
				await result;
			}
            catch (Exception e)
            {
				await parent.AssignedAction.ShowAlert();
                StreamDeck.LogException(e);
            }
            StreamDeck.LogMessage($"Successfully executed payload of type {payloadType.Name}.");
		}

	}
}
