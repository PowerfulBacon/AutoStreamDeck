
using AutoStreamDeck;
using System.Diagnostics;

// Setup logging to a file
File.WriteAllText("log.txt", "");
StreamDeck.OnStatusMessage += msg => File.AppendAllText("log.txt", $"{msg}\n");
StreamDeck.OnInternalError += e => File.AppendAllText("log.txt", $"{e.ToString()}\n");

// If the debugger is attached, then build the plugin and load it with streamdeck instead.
if (Debugger.IsAttached)
{
	await StreamDeck.BuildAndReloadPlugin(new AutoStreamDeck.Objects.PluginInformation() {
		Author = "PowerfulBacon",
		PluginName = "Example Plugin",
		Description = "An example plugin to show off the capabilities of AutoStreamDeck."
	});
	return;
}

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
