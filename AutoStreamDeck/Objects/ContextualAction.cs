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
	internal class ContextualAction
	{

		/// <summary>
		/// Reflected action types
		/// </summary>
		private static Dictionary<Type, Type> ActionTypes = AppDomain.CurrentDomain.GetAssemblies()
			.SelectMany(x => x.GetTypes())
			.Where(x => x.GetCustomAttribute<ActionMetaAttribute>() != null)
			.ToDictionary(x => x, x => x.BaseType!.GetGenericArguments()[0]);

		/// <summary>
		/// List all the actions by their name
		/// </summary>
		private static Dictionary<string, (Type actionType, Type settingsType)> ActionsByName = ActionTypes.ToDictionary(
			x => ActionHelpers.ActionNameToURI(x.Key.GetCustomAttribute<ActionMetaAttribute>()!.ActionName),
			x => (actionType: x.Key, settingsType: x.Value)
		);

		public string ContextID { get; }

		public string ActionID { get; }

		public Type SettingsType { get; }

		public ISDAction AssignedAction { get; }

		/// <summary>
		/// Dictionary containing a list of all the events belonging to this contextual action.
		/// These objects hold:
		/// - Payload type
		/// - Event Type
		/// </summary>
		public Dictionary<string, ContextualEvent> contextualEvents = new Dictionary<string, ContextualEvent>();

		public ContextualAction(string contextID, string actionID)
		{
			ContextID = contextID;
			ActionID = actionID;
			// Determine the settings type
			SettingsType = ActionsByName[actionID].settingsType;
			// Create and set the ISDAction
			AssignedAction = (ISDAction)(Activator.CreateInstance(ActionsByName[actionID].actionType) ?? throw new NullReferenceException($"Could not create an instance of {ActionsByName[actionID].actionType.Name}"));
			AssignedAction.Context = contextID;
		}

		public object CreateSettingsObject(JsonNode node)
		{
			return JsonSerializer.Serialize(node, SettingsType);
		}

		public async Task HandleEvent(string eventName, JsonNode payload)
		{
			// We aren't listening for this event, so don't do anything
			if (!ContextualEvent.EventTypes.ContainsKey(eventName))
				return;
			if (contextualEvents.TryGetValue(eventName, out var contextualEvent))
			{
				await contextualEvent.Raise(payload);
			}
			else
			{
				ContextualEvent createdContextualEvent = new(this, eventName);
				contextualEvents.Add(eventName, createdContextualEvent);
				await createdContextualEvent.Raise(payload);
			}
		}

	}
}
