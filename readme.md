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

	public override async Task OnKeyDown(string context,KeyDownPayload<RandomNumberSettings> keyDownEvent)
	{
		Random random = new Random();
		await SetTitle(random.Next(1, 7).ToString());
	}

}
```

## Future Work

- Mac support
- Completion of the API (https://docs.elgato.com/sdk/plugins/events-received and https://docs.elgato.com/sdk/plugins/events-sent)
