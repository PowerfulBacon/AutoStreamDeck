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
		private static Dictionary<Type, Type> ActionTypes;

		/// <summary>
		/// List all the actions by their name
		/// </summary>

		private static Dictionary<string, Tuple<Type, Type>> ActionsByName;


        internal static Dictionary<string, Tuple<Type, Type>> GetActionsByName()
		{
			if (ActionsByName != null)
			{
				return ActionsByName;
			}
			ActionTypes = ReflectionHelpers.ReflectedAssemblies
				.SelectMany(x => x.GetTypes())
				.Where(x => x.CustomAttributes.Where(y => y.AttributeType == typeof(ActionMetaAttribute)).FirstOrDefault() != null)
				.ToDictionary(x => x, x => (x.BaseType.GetGenericArguments().Length > 0 ? x.BaseType.GetGenericArguments()[0] : typeof(NoSettings)));
			ActionsByName = ActionTypes.ToDictionary(
				x => ActionHelpers.MakeStringPath((string)x.Key.CustomAttributes.Where(y => y.AttributeType == typeof(ActionMetaAttribute)).First().ConstructorArguments[0].Value),
				x => new Tuple<Type, Type>(x.Key, x.Value)
			);
			StreamDeck.LogMessage($"Loaded {ActionTypes.Count} actions from the following assemblies: {string.Join(", ", ReflectionHelpers.ReflectedAssemblies.Select(x => x.FullName))}");
			return ActionsByName;
		}

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
			string actionName = actionID.Substring(actionID.LastIndexOf(".") + 1);
			var actions = GetActionsByName();
			SettingsType = actions[actionName].Item2;
			// Create and set the ISDAction
			AssignedAction = (ISDAction)(Activator.CreateInstance(actions[actionName].Item1) ?? throw new NullReferenceException($"Could not create an instance of {actions[actionName].Item1.Name}"));
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
			{
				StreamDeck.LogMessage($"Could not locate the event type with name {eventName}. Found: {string.Join(", ", ContextualEvent.EventTypes.Keys)}");
				return;
			}
			if (contextualEvents.TryGetValue(eventName, out var contextualEvent))
			{
				await contextualEvent.Raise(payload);
			}
			else
			{
				ContextualEvent createdContextualEvent = new ContextualEvent(this, eventName);
				contextualEvents.Add(eventName, createdContextualEvent);
				await createdContextualEvent.Raise(payload);
			}
		}

	}
}
