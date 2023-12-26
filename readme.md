# StreamDeckNet

**Custom streamdeck plugins made easy.**
A framework for automatically generating plugins for the StreamDeck.

What you need to handle:
- Calling the framework's launching functions
- Creating the code for handling events

What this framework handles:
- Generation of the manifest file
- Installing the plugin into streamdeck
- Communication between the plugin and the streamdeck.

This framework is still in development and not all features of the streamdeck API have been implemented yet.

## Getting Started

Add this package to your C# project.
Note that this will generate executable files that must be launched by the streamdeck application. This means that if you want to create plugins that integrate with an existing app, your plugin will not be launched from inside the existing app but will need to serve as a middleware between the existing app and your plugin.

### Plugin Launcher

Create an executable console application in C#, and add the following code:
```cs
using StreamDeckNet;
using System.Diagnostics;

// If the debugger is attached, then build the plugin and load it with streamdeck instead.
if (Debugger.IsAttached)
{
	await StreamDeck.BuildAndReloadPlugin();
	return;
}

// Setup logging to a file
File.WriteAllText("log.txt", "");
StreamDeck.OnStatusMessage += msg => File.AppendAllText("log.txt", $"{msg}\n");
StreamDeck.OnInternalError += e => File.AppendAllText("log.txt", $"{e.ToString()}\n");

try
{
	// Wait for the application to launch
	Task webSocketTask = await StreamDeck.LaunchPlugin(args);
	// Wait for the web-socket to close, so that the program doesn't instantly terminate itself.
	await webSocketTask;
}
catch (Exception e)
{
	// Log the exception to the log file
	File.AppendAllText("log.txt", $"{e.ToString()}\n");
	// Terminate with an error code
	Environment.Exit(-1);
}
```

This code does serveral things:
- It logs any informational messages or errors to a file called log.txt. This file will exist inside of your AppData/Roaming/Elgato/StreamDeck/Plugins/pluginname directory.
- If you are running the project with a debugger attached (from Visual Studio or Rider directly) then it will install the application to, and restart, streamdeck.
- If the program was launched normally (by streamdeck), then it will launch the plugin and wait for the plugin to finish (the web-socket task will run until streamdeck closes).

If you do not want to automatically install the plugin into streamdeck, then you can ignore the part that calls `BuildAndReloadPlugin`. Instead call `BuildPlugin` and pass in the directory of where your built plugin should be stored. For example: `await StreamDeck.BuildPlugin("C:/users/username/Desktop/plugin_project")`.

### Plugin Actions

To create actions you need to do the following things:
- Create a class that inherits from `SDAction`. SDAction can optionally take in a generic parameter, which is the configurable settings for this action. Without this generic parameter, the action will not be configutable.
- Add the `ActionMeta` attribute to the created SDAction subtype.
- If you want the action to have configurable settings in StreamDeck, then create a class that contains your settings.

Example with settings:
```cs
[ActionMeta("Random Number", Description = "Generates a random number and sets the title of the button to the number.")]
internal class RandomNumberAction : SDAction<RandomNumberSettings>
{

	public override async Task OnKeyDown(string context,KeyDownPayload<RandomNumberSettings> keyDownEvent)
	{
		Random random = new Random();
		await SetTitle(random.Next(keyDownEvent.Settings.Minimum, keyDownEvent.Settings.Maximum + 1).ToString());
	}

}

internal class RandomNumberSettings
{
	public int Minimum = 1;
	public int Maximum = 6;
}
```

Example without settings:
```cs
[ActionMeta("Random Number", Description = "Generates a random number and sets the title of the button to the number.")]
internal class RandomNumberAction : SDAction
{

	public override async Task OnKeyDown(string context,KeyDownPayload<NoSettings> keyDownEvent)
	{
		Random random = new Random();
		await SetTitle(random.Next(1, 7).ToString());
	}

}
```

## External Application Integration

(Terminology Note: External-application may also be a plugin for an external-application).

With this plugin you can directly connect the stream deck application with a plugin an external application, such as Unity.
Doing this requires going through a relay plugin.

Process:
1. The stream-deck launches the relay plugin with a specified identifier.
2. The external application connects to the background relay with the same identifier.
3. The stream-deck plugin sends the port number of the web-socket to the external application.
4. The external application connects directly to the stream-deck application.
5. The external application disconnects from the relay plugin's server.

To achieve this in-code, both a stream-deck plugin and the external application need to interface with the library.

**All of the SDActions need to be in the external application and the stream-deck plugin, it is recommended that the SDActions are placed in a shared project that both the plugin and the external application can have as references.**
You may also have the external application and stream-deck plugin be launched from the same executable by checkinng the command line arguments passed and calling `StreamDeckRelay.ConnectToBackgroundRelay` or `StreamDeckRelay.LaunchRelayPlugin` accordingly.

### External Application

Next, have the external application connect to the stream-deck in the background.
This process cannot be awaited and will run in the background.
This process may not instantly connect to a stream-deck, if there is no stream-deck running then the process will attempt to connect every 5 seconds while the external application is running.

```cs
StreamDeckRelay.ConnectToBackgroundRelay(unique_id);
```

Once this has connected, the stream-deck actions may be invoked by the stream-deck which you can setup to trigger behaviours in your external application.
For example, an action could be created which calls some debug code in a Unity game.

### Stream Deck Plugin

The stream-deck plugin is responsible for telling the external application how to connect to the stream-deck. This application also needs to be aware of all SDActions because they are collected via reflection to build the manifest file.

The relay-plugin should have the following code, which will launch the relay server and allow an external application to connect.

```cs
// The plugin should connect to, or launch, a relay server so that it can communicate with the external application.
Task relayServerTask = await StreamDeckRelay.LaunchRelayPlugin(unique_id, args);
// Wait for the stream-deck application to be terminated, as the stream-deck application will automatically restart any plugins that terminate.
await StreamDeckRelay.WaitForTermination;
```

Note that if multiple plugins are using the relay server, they will cooperate and share a single relay-server.

## Future Work

- Completion of the API (https://docs.elgato.com/sdk/plugins/events-received and https://docs.elgato.com/sdk/plugins/events-sent)
- Mac & Linux support
