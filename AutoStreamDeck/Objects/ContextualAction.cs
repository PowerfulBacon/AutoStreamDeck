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
#if NET8_0
		private static Dictionary<Type, Type> ActionTypes;
#else
		private static Dictionary<Type, Type> ActionTypes;
#endif

		/// <summary>
		/// List all the actions by their name
		/// </summary>
#if NET8_0
		private static Dictionary<string, (Type actionType, Type settingsType)> ActionsByName;
#else
		private static Dictionary<string, (Type actionType, Type settingsType)> ActionsByName = ActionTypes.ToDictionary(
			x => ActionHelpers.MakeStringPath(x.Key.GetCustomAttribute<ActionMetaAttribute>().ActionName),
			x => (actionType: x.Key, settingsType: x.Value)
		);
#endif

#if NET8_0
		internal static Dictionary<string, (Type actionType, Type settingsType)> GetActionsByName(Assembly[]? additionalAssemblies)
#else
		internal static Dictionary<string, (Type actionType, Type settingsType)> GetActionsByName(Assembly[] additionalAssemblies)
#endif
		{
			if (ActionsByName != null)
			{
				return ActionsByName;
			}
#if NET8_0
			ActionTypes = (additionalAssemblies ?? new Assembly[0])
				.Append(Assembly.GetEntryAssembly())
				.SelectMany(x => x.GetTypes())
				.Where(x => x.GetCustomAttribute<ActionMetaAttribute>() != null)
				.ToDictionary(x => x, x => (x.BaseType!.GetGenericArguments().Length > 0 ? x.BaseType!.GetGenericArguments()[0] : typeof(NoSettings)));
			ActionsByName = ActionTypes.ToDictionary(
				x => ActionHelpers.MakeStringPath(x.Key.GetCustomAttribute<ActionMetaAttribute>()!.ActionName),
				x => (actionType: x.Key, settingsType: x.Value)
			);
#else
			ActionTypes = (additionalAssemblies ?? new Assembly[0])
				.Append(Assembly.GetEntryAssembly())
				.SelectMany(x => x.GetTypes())
				.Where(x => x.GetCustomAttribute<ActionMetaAttribute>() != null)
				.ToDictionary(x => x, x => (x.BaseType.GetGenericArguments().Length > 0 ? x.BaseType.GetGenericArguments()[0] : typeof(NoSettings)));
			ActionsByName = ActionTypes.ToDictionary(
				x => ActionHelpers.MakeStringPath(x.Key.GetCustomAttribute<ActionMetaAttribute>().ActionName),
				x => (actionType: x.Key, settingsType: x.Value)
			);
#endif
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

		public ContextualAction(string contextID, string actionID, Assembly[] additionalAssemblies)
		{
			ContextID = contextID;
			ActionID = actionID;
			// Determine the settings type
			string actionName = actionID.Substring(actionID.LastIndexOf(".") + 1);
			var actions = GetActionsByName(additionalAssemblies);
			SettingsType = actions[actionName].settingsType;
			// Create and set the ISDAction
			AssignedAction = (ISDAction)(Activator.CreateInstance(actions[actionName].actionType) ?? throw new NullReferenceException($"Could not create an instance of {actions[actionName].actionType.Name}"));
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
				ContextualEvent createdContextualEvent = new ContextualEvent(this, eventName);
				contextualEvents.Add(eventName, createdContextualEvent);
				await createdContextualEvent.Raise(payload);
			}
		}

	}
}
