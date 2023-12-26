using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace AutoStreamDeck
{
	/// <summary>
	/// Class for managing stream deck relay, the relay service which allows for
	/// applications to directly integrate with the streamdeck.
	/// 
	/// Architecture:
	/// Stream Deck <-Launch-> Relay Plugin <-Local TCP-> StreamDeck Plugin <-Integration-> Application
	/// 
	/// The application launches the background relay by calling ConnectToBackgroundRelay.
	/// The stream deck launches the relay plugin.
	/// The relay plugin sends a message to the background relay which instructs it to connect directly to the streamdeck
	/// application.
	/// The streamdeck is now directly connected to the target application's plugin without a middle websocket.
	/// </summary>
	public static class StreamDeckRelay
	{

		// This is a kind of janky way of seeing if our parent stream deck application has closed so that we can properly terminate ourselves
		// when the streamdeck application closes.
		// The normal stream deck direct connection can terminate when the websocket closes, but we don't have that luxury.
		private static Process[] parentProcess = Process.GetProcessesByName("StreamDeck");

		private static bool HasTerminated = parentProcess.All(x => x.HasExited);

		public static Task WaitForTermination = parentProcess.ForEach(async x => await x.WaitForExitAsync());

		/// <summary>
		/// Launch the plugin as a relay program.
		/// If a relay server already exists, then it will connect to that and broadcast its plugin.
		/// If a relay server doesn't exist, then it launches the relay server.
		/// </summary>
		/// <param name="relayIdentifier"></param>
		/// <returns></returns>
		public static async Task<Task> LaunchRelayPlugin(string relayIdentifier, string[] args, int relayServerPort = 17423)
		{
			if (HasTerminated)
			{
				StreamDeck.LogMessage("The stream-deck applicaiton has already been closed.");
				return Task.CompletedTask;
			}
			try
			{
				TcpClient tcpClient = new TcpClient();
				int relayOffset = 0;
				try
				{
					// Continue connecting to servers
					while (HasTerminated)
					{
						// Connect to the relay
						await tcpClient.ConnectAsync("localhost", relayServerPort + relayOffset);
						StreamDeck.LogMessage($"Attempting to locate relay plugin server at localhost:{relayServerPort + relayOffset}...");
						// If we weren't able to connect, then we need to host the server ourself.
						if (!tcpClient.Connected)
							break;
						StreamDeck.LogMessage($"Connected to server on localhost:{relayServerPort + relayOffset}, ensuring that this is a relay plugin server...");
						using (var stream = tcpClient.GetStream())
						{
							string message = $"BCST;{relayIdentifier};{string.Join(";", relayIdentifier)}";
							await SendString(stream, message);
							// Make sure that we are actualy connected to the right thing
							stream.ReadTimeout = 2000;
							byte[] recievedMessage = new byte[1024];
							try
							{
								int recieved = await stream.ReadAsync(recievedMessage, 0, 1024);
								string stringMessage = Encoding.ASCII.GetString(recievedMessage, 0, recieved);
								// Ensure that the message is a correct message
								switch (stringMessage)
								{
									case "RELAY_ACCEPT":
										// We have broadcasted our precense to the server, we can quit now.
										return Task.CompletedTask;
									case "RELAY_REJECTED":
										// The server we connected to is a relay server, but the data we provided was rejected
										throw new ArgumentException("Attempting to launch a streamdeck plugin without the -port or -pluginUUID arguments. This may indicate that the plugin was launched from outside of the stream deck program. " +
											$"Try installing the created plugin into streamdeck by calling StreamDeckNet.BuildPlugin. The passed arguments were: {string.Join(",", args)}");
									default:
										StreamDeck.LogMessage($"Server on port {relayServerPort + relayOffset - 1} responded to our broadccast with an incorrect message which indicates that it may not be a relay plugin server. The message was: {stringMessage}. Checking next port...");
										relayOffset++;
										continue;
								}
							}
							catch (TimeoutException)
							{
								// Assume timeout and continue
								StreamDeck.LogMessage($"Server on port {relayServerPort + relayOffset - 1} did not respond to our broadcast in time meaning that it may not be a relay plugin server. Checking next port...");
								relayOffset++;
								continue;
							}
						}
					}
				}
				catch (SocketException)
				{ }
				StreamDeck.LogMessage($"Plugin server could not be located, launching plugin server on port {relayServerPort + relayOffset}.");
				// Things that we need to remember for the server
				Dictionary<string, string[]> waitingServers = new() {
					{ relayIdentifier, args }
				};
				Dictionary<string, TcpClient> waitingClients = new();
				// If we got to this point, then we have failed to locate the plugin server. This means that we need to host one ourself.
				TcpListener pluginServer = new TcpListener(IPAddress.Loopback, relayServerPort + relayOffset);
				pluginServer.Start();
				return Task.Run(async () =>
				{
					try
					{
						while (!HasTerminated)
						{
							// Accept the TcpClient
							StreamDeck.LogMessage($"Waiting for plugin client to connect...");
							TcpClient client = await pluginServer.AcceptTcpClientAsync();
							StreamDeck.LogMessage($"Plugin client on port {client.Client.RemoteEndPoint} connected.");
							_ = Task.Run(async () =>
							{
								using (var stream = client.GetStream())
								{
									while (client.Connected)
									{
										byte[] byteStream = new byte[1 << 16];
										int recievedCount;
										try
										{
											recievedCount = await stream.ReadAsync(byteStream, 0, 1 << 16);
										}
										catch (IOException)
										{
											StreamDeck.LogMessage($"Plugin relay client disconnected.");
											return;
										}
										string receivedString = Encoding.ASCII.GetString(byteStream, 0, recievedCount);
										// Parse the message
										string[] receivedParts = receivedString.Split(";");
										switch (receivedParts[0])
										{
											// Broadcast comes from other plugins that want someone to connect to them
											case "BCST":
												string relayIdentifier = receivedParts[1];
												StreamDeck.LogMessage($"Plugin is broadcasting on '{relayIdentifier}'.");
												// Check if someone is waiting for this relay
												if (waitingClients.TryGetValue(relayIdentifier, out var waitingClient))
												{
													if (waitingClient.Connected)
													{
														using (var clientStream = waitingClient.GetStream())
														{
															// Instruct the relay that they should connect to this address
															await SendString(clientStream, $"CNCT;{string.Join(";", receivedParts.Skip(2))}");
														}
													}
													waitingClients.Remove(relayIdentifier);
													await SendString(stream, "RELAY_ACCEPT");
													return;
												}
												// Store information about this relay so that we can give it to whoever wants it.
												waitingServers.Add(relayIdentifier, receivedParts.Skip(2).ToArray());
												await SendString(stream, "RELAY_ACCEPT");
												break;
											// Request comes from the client
											case "RQST":
												string requested = receivedParts[1];
												StreamDeck.LogMessage($"Client requested '{requested}'.");
												if (waitingServers.TryGetValue(requested, out var waitingServer))
												{
													// Instruct the relay that they should connect to this address
													await SendString(stream, $"CNCT;{string.Join(";", waitingServer)}");
													// We found our target
													waitingServers.Remove(requested);
												}
												await SendString(stream, "NFND");
												break;
										}
									}
								}
								StreamDeck.LogMessage($"Plugin client on port {client.Client.RemoteEndPoint} disconnected.");
							})
							// If the task faults, then log the exception
							.ContinueWith(task =>
							{
								StreamDeck.LogException(task.Exception!);
							}, TaskContinuationOptions.OnlyOnFaulted);
						}
						StreamDeck.LogMessage($"Plugin server on port {relayServerPort + relayOffset} shut down.");
					}
					catch (Exception e)
					{
						StreamDeck.LogMessage($"Plugin server on port {relayServerPort + relayOffset} encountered a fatal exception and shut down.");
						StreamDeck.LogException(e);
					}
				})
				// If the task faults, then log the exception
				.ContinueWith(task =>
				{
					StreamDeck.LogException(task.Exception!);
				}, TaskContinuationOptions.OnlyOnFaulted);
			}
			catch (Exception e)
			{
				StreamDeck.LogException(e);
				return Task.CompletedTask;
			}
		}

		/// <summary>
		/// Connect to the background relay
		/// </summary>
		/// <param name="relayIdentifier"></param>
		public static void ConnectToBackgroundRelay(string relayIdentifier, int relayServerPort = 17423)
		{
			StreamDeck.LogMessage($"StreamDeck plugin relay initialised on port {relayServerPort}. Waiting for connection...");
			Task.Run(async () => {
				int serverPortOffset = 0;
				while (true)
				{
mainServerLoop:
					// Try to connect to the server, if that fails then try again in 5 seconds
					TcpClient client = new TcpClient();
					client.ReceiveTimeout = 5000;
					try
					{
						// Attempt to connect to the stream deck relay server
						await client.ConnectAsync("localhost", relayServerPort + serverPortOffset);
						// Unable to connect, wait for 5 seconds to retry
						if (!client.Connected)
						{
							await Task.Delay(5000);
							continue;
						}
						StreamDeck.LogMessage($"Successfully connected to a server, ensuring that it is a relay server...");
						// Ensure that we are actually talking to the correct thing
						using (var stream = client.GetStream())
						{
							// Tell the server that we are asking for a certain relay
							await SendString(stream, $"RQST;{relayIdentifier}");
							while (client.Connected)
							{
								// Get the response
								byte[] byteStream = new byte[1 << 16];
								int recievedCount = await stream.ReadAsync(byteStream, 0, 1 << 16);
								if (recievedCount == 0)
								{
									// Server exists, but probably isn't a plugin relay.
									serverPortOffset++;
									StreamDeck.LogMessage("The relay server timed out, it is probably not a relay server. Checking next port...");
									goto mainServerLoop;
								}
								string receivedString = Encoding.ASCII.GetString(byteStream, 0, recievedCount);
								// Parse the message
								string[] receivedParts = receivedString.Split(";");
								// Check if that relay exists, if it doesn't then we just need to wait until it comes online
								// If we do not get a valid response, then this isn't a relay server so check the next port
								switch (receivedParts[0])
								{
									case "CNCT":
										// Fetch the argument information from the server
										string[] arguments = receivedParts.Skip(1).ToArray();
										// Connect to the specified websocket
										StreamDeck.LogMessage("We have found our plugin, connecting to websocket and disconnecting from relay.");
										Task pluginTask = await StreamDeck.LaunchPlugin(arguments);
										client.Close();
										StreamDeck.LogMessage("Cleaned up!");
										await pluginTask;
										return;
									case "NFND":
										StreamDeck.LogMessage("The stream deck plugin that we wanted was not online. Waiting for response");
										continue;
									default:
										// Server exists, but probably isn't a plugin relay.
										serverPortOffset++;
										StreamDeck.LogMessage("The relay server returned an invalid response, it is probably not a relay server. Checking next port...");
										goto mainServerLoop;
								}
							}
						}
					}
					catch (TimeoutException)
					{
						// Server exists, but probably isn't a plugin relay.
						serverPortOffset++;
						StreamDeck.LogMessage("The relay server timed out, it is probably not a relay server. Checking next port...");
						continue;
					}
					catch (SocketException)
					{
						// Unable to connect, wait for 5 seconds to retry.
						await Task.Delay(5000);
						continue;
					}
					// Unable to connect, wait for 5 seconds to retry.
					await Task.Delay(5000);
					continue;
				}
			})
			// If the task faults, then log the exception
			.ContinueWith(task => {
				StreamDeck.LogException(task.Exception!);
			}, TaskContinuationOptions.OnlyOnFaulted);
		}

		private static async Task SendString(NetworkStream stream, string message)
		{
			byte[] byteStream = Encoding.ASCII.GetBytes(message);
			await stream.WriteAsync(byteStream);
		}

	}
}
