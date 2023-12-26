using AutoStreamDeck.Events.SendEvents;
using AutoStreamDeck.Extensions;
using AutoStreamDeck.Objects;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace AutoStreamDeck
{
    /// <summary>
    /// 
    /// </summary>
    public static class StreamDeck
	{

		/// <summary>
		/// Called when a status message is dispatched.
		/// </summary>
		public static event Action<string>? OnStatusMessage;

		/// <summary>
		/// Called when the application has an internal error.
		/// </summary>
		public static event Action<Exception>? OnInternalError;

		/// <summary>
		/// Socket that we use to communicate with the stream deck application
		/// </summary>
		private static ClientWebSocket? communicationSocket;

		/// <summary>
		/// Dictionary containing all contexts by their UUID
		/// </summary>
		private static Dictionary<(string, string), ContextualAction> Contexts = new();

		/// <summary>
		/// Launch the plugin with the specified arguments from the streamdeck.
		/// </summary>
		/// <return>Returns the task that manages the receving and dispatching of stream-deck actions.</return>
		/// <param name="applicationArguments">The argments passed in from the streamdeck</param>
		public static async Task<Task> LaunchPlugin(string[] applicationArguments)
		{
			string? port = null;
			string? uuid = null;
			string? registerEvent = null;
			string? info = null;
			// Parse the application arguments
			for (int i = 0; i < applicationArguments.Length; i++)
			{
				switch (applicationArguments[i])
				{
					case "-port":
						port = applicationArguments[++i];
						break;
					case "-pluginUUID":
						uuid = applicationArguments[++i];
						break;
					case "-registerEvent":
						registerEvent = applicationArguments[++i];
						break;
				}
			}
			// Ensure that we have the required parameters
			if (port == null || uuid == null || registerEvent == null)
				throw new ArgumentException("Attempting to launch a streamdeck plugin without the -port or -pluginUUID arguments. This may indicate that the plugin was launched from outside of the stream deck program. " +
					$"Try installing the created plugin into streamdeck by calling StreamDeckNet.BuildPlugin. The passed arguments were: {string.Join(",", applicationArguments)}");
			// Initial setup
			Contexts = new();
			// Start connection procedure
			LogMessage($"Connecting to ws://localhost:{port}...");
			// Connect to the server
			Uri communicationUri = new($"ws://localhost:{port}");
			ClientWebSocket websocket = new();
			await websocket.ConnectAsync(communicationUri, default);
			LogMessage($"Connection successful, starting plugin subtask.");
			// Register the property inspector
			byte[] registration = Encoding.UTF8.GetBytes(@$"{{""event"":""{registerEvent}"",""uuid"":""{uuid}""}}");
			await websocket.SendAsync(registration, WebSocketMessageType.Text, true, default);
			// Start the listener task
			return Task.Run(async () => {
				StringBuilder currentResult = new StringBuilder();
				communicationSocket = websocket;
				while (websocket.State == WebSocketState.Open)
				{
					var bytes = new byte[1024];
					var result = await websocket.ReceiveAsync(bytes, default);
					currentResult.Append(Encoding.UTF8.GetString(bytes, 0, result.Count));
					if (result.EndOfMessage)
					{
						string resultText = currentResult.ToString();
						currentResult.Clear();
						try
						{
							// Get a JSON DOM and parse the message
							JsonNode? message = JsonNode.Parse(resultText);
							// No, or invalid, message
							if (message == null)
								continue;
							// Get the action
							string? action = message["action"]?.GetValue<string>();
							string? eventName = message["event"]?.GetValue<string>();
							string? context = message["context"]?.GetValue<string>();
							JsonNode? payload = message["payload"];
							if (context == null || action == null || payload == null || eventName == null)
								continue;
							LogMessage($"Handling contextual action ({context}, {action}).");
							// Handle the event
							ContextualAction locatedContext = CreateOrGetContextualAction(context, action);
							await locatedContext.HandleEvent(eventName, payload);
						}
						catch (Exception e)
						{
							LogException(e);
						}
					}
					else
					{
						LogMessage("Recieved partial message, waiting for next bit...");
					}
				}
				Contexts.Clear();
				communicationSocket = null;
				LogMessage("StreamDeck disconnected, terminating web-socket.");
				websocket.Dispose();
			});
		}

		/// <summary>
		/// Get the context requested, or create it if it doesn't already exit.
		/// </summary>
		/// <param name="context"></param>
		/// <returns></returns>
		private static ContextualAction CreateOrGetContextualAction(string context, string action)
		{
			// Return the cached context
			if (Contexts.TryGetValue((context, action), out var val))
				return val;
			// Create a new context
			ContextualAction createdContext = new ContextualAction(context, action);
			Contexts.Add((context, action), createdContext);
			return createdContext;
		}

		/// <summary>
		/// Dispatch an event to the stream deck.
		/// </summary>
		/// <param name="sdEvent"></param>
		internal static async Task DispatchEvent<TEvent>(TEvent sdEvent)
			where TEvent : SendEvent
		{
			if (communicationSocket == null)
			{
				LogMessage("Attempting to dispatch an event to the server, however there is no server listener.");
				return;
			}
			string jsonSerialised = JsonSerializer.Serialize(sdEvent);
			LogMessage($"Dispatching {typeof(TEvent).Name} event...\nPayload: {jsonSerialised}");
			byte[] registration = Encoding.UTF8.GetBytes(jsonSerialised);
			await communicationSocket.SendAsync(registration, WebSocketMessageType.Text, true, default);
		}

		/// <summary>
		/// Call the message logging functionality.
		/// </summary>
		/// <param name="message"></param>
		/// <exception cref="AggregateException"></exception>
		internal static void LogMessage(string message)
		{
			try
			{
				OnStatusMessage?.Invoke(message);
			}
			catch (Exception ex)
			{
				LogException(ex);
			}
		}

		/// <summary>
		/// Call the error logging functionality.
		/// </summary>
		/// <param name="ex"></param>
		/// <exception cref="AggregateException"></exception>
		internal static void LogException(Exception ex)
		{
			try
			{
				OnInternalError?.Invoke(ex);
			}
			catch (Exception fatalException)
			{
				throw new AggregateException("An exception was thrown inside of the error handler resulting in a fatal exception.", fatalException);
			}
		}

		/// <summary>
		/// Builds the created plugin, creating all the necessary files for the plugin to function.
		/// Use this when in development mode to add the plugin to the streamdeck's plugin list.
		/// </summary>
		/// <param name="streamDeckPath">An optional path to the streamdeck application plugins directory, defaults to C:\Users\%Username%\AppData\Roaming\Elgato\StreamDeck\Plugins</param>
		public static async Task BuildPlugin(PluginInformation pluginInformation, string? streamDeckPath = null)
		{
			if (streamDeckPath == null)
				streamDeckPath = $"C:\\Users\\{Environment.UserName}\\AppData\\Roaming\\Elgato\\StreamDeck\\Plugins";
			if (!Path.Exists(streamDeckPath))
				throw new FileNotFoundException($"The streamdeck's plugins folder could not be found at '{streamDeckPath}'. If your stream deck plugins are not installed on the C: drive, please manually enter the plugin directory.");
			// Create the plugin directory
			string pluginUri = $"com.{ActionHelpers.MakeStringPath(pluginInformation.Author)}.{ActionHelpers.MakeStringPath(pluginInformation.PluginName)}";
			string pluginPath = $"{streamDeckPath}\\{pluginUri}.sdPlugin";
			if (!Path.Exists(pluginPath))
				Directory.CreateDirectory(pluginPath);
			else
			{
				for (int attempts = 5; attempts > 0; attempts--)
				{
					try
					{
						Directory.EnumerateFiles(pluginPath, "*.*")
							.ForEach(File.Delete);
						break;
					}
					catch (Exception e)
					{
						if (attempts == 1)
						{
							throw;
						}
						await Task.Delay(1000 * (6 - attempts));
					}
				}
			}
			// Get all of the variables we need to build the manifest
			string manifestFile = File.ReadAllText("Templates/manifest.json");
			string actionTemplate = File.ReadAllText("Templates/action.json");
			// Perform necessary replacements
			manifestFile = manifestFile
				.Replace("%VERSION%", pluginInformation.Version)
				.Replace("%AUTHOR%", pluginInformation.Author)
				.Replace("%CODEPATH%", Path.GetFileNameWithoutExtension(Path.GetRelativePath(".", Assembly.GetEntryAssembly()!.Location)))
				.Replace("%DESCRIPTION%", pluginInformation.Description)
				.Replace("%NAME%", pluginInformation.PluginName)
				.Replace("%URI%", pluginUri)
				.Replace("%ACTIONS%", string.Join(",\n", ContextualAction.ActionsByName
					.Select(action => {
						return actionTemplate
							.Replace("%URI%", pluginUri)
							.Replace("%NAME%", action.Key)
							.Replace("%UUID%", action.Key);
					})));
			// Inject the manifest file into the C# directory
			File.WriteAllText($"{pluginPath}\\manifest.json", manifestFile);
			// Copy the application across
			Directory.EnumerateFiles(".", "*.*")
				.ForEach(file =>
				{
					File.Copy(file, $"{pluginPath}\\{Path.GetFileName(file)}");
				});
		}

		/// <summary>
		/// Builds the plugin, adds it to the stream-deck application and then forces streamdeck to restart.
		/// </summary>
		/// <param name="streamDeckApplicationPath">An optional path to the streamdeck application executable. Defaults to C:\Program Files\Elgato\StreamDeck\StreamDeck.exe</param>
		/// <param name="streamDeckPath">An optional path to the streamdeck application plugins directory, defaults to C:\Users\%Username%\AppData\Roaming\Elgato\StreamDeck\Plugins</param>
		public static async Task BuildAndReloadPlugin(PluginInformation pluginInformation, string? streamDeckApplicationPath = null, string? streamDeckPlugins = null)
		{
			// Find and kill the application
			await Process.GetProcessesByName("StreamDeck")
				.ForEach(async process => {
					process.Kill();
					await process.WaitForExitAsync();
				});
			// Wait 2 seconds to allow for the file handler to be cleared
			await Task.Delay(2000);
			// Build the plugin
			await BuildPlugin(pluginInformation, streamDeckPlugins);
			// Launch the streamdeck application
			Process.Start(streamDeckApplicationPath ?? "C:\\Program Files\\Elgato\\StreamDeck\\StreamDeck.exe");
		}

	}
}
