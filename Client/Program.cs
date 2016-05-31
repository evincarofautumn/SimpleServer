using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading;

namespace Client
{
	class ClientMain
	{

		private TcpClient client;

		public static void Main (string[] args)
		{
			var client = new ClientMain ();
			client.Run ().Wait ();
		}

		public ClientMain ()
		{
			client = new TcpClient ();
		}

		public Task Run ()
		{
#if DEBUG
			Console.WriteLine ("[client] trying to connect to localhost:{0}", Server.ServerMain.Port);
#endif
			int retries = 3;
			int delay = 10;
			while (true) {
				try {
					client.Connect ("localhost", Server.ServerMain.Port);
					break;
				} catch (SocketException) {
					Console.Error.WriteLine ("[client] ERROR connecting to server, retrying");
					Thread.Sleep (delay);
					delay *= 10;
					if (retries == 0) {
						Console.Error.WriteLine ("[client] ERROR connecting to server, no retries left");
						System.Environment.Exit (1);
					}
				}
			}
#if DEBUG
			Console.WriteLine ("[client] connected");
#endif
//			client.ReceiveTimeout = 5000;
//			client.SendTimeout = 5000;
			return Task.Run (() => {
				var serverStream = client.GetStream ();
				var request = new byte [Server.ServerMain.RequestLength];
				var response = new byte [Server.ServerMain.ResponseLength];
				for (int i = 0; i < Server.ServerMain.Requests; ++i) {
#if DEBUG
					Console.WriteLine ("[client] sending request #{0}", i);
#endif
					// Send request.
					serverStream.Write (request, 0, Server.ServerMain.RequestLength);
#if DEBUG
					Console.WriteLine ("[client] sent request #{0}, waiting for response #{0}", i);
#endif
					// Wait for response.
					serverStream.Read (response, 0, Server.ServerMain.ResponseLength);
#if DEBUG
					Console.WriteLine ("[client] recieved response #{0}", i);
#endif
				}
#if DEBUG
				Console.WriteLine ("[client] exchanged {0} request/response pairs, sending terminate request", Server.ServerMain.Requests);
#endif
				request = new byte [] { Server.ServerMain.EOT };
				serverStream.Write (request, 0, 1);
				serverStream.Flush ();
#if DEBUG
				Console.WriteLine ("[client] sent terminate request");
#endif
				client.Close ();
			});
		}
	}
}
