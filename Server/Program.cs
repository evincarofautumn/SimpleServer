using System;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Diagnostics;

namespace Server
{
	public class ServerMain
	{
		public const int RequestLength = 512;
		public const int ResponseLength = 512;
		public const int Port = 12421;

		private int workers;

		public const int Requests = 10000;
		public const byte EOT = (byte)'\x04';

		private List<Task> workerTasks;
		private Stopwatch stopwatch;

		public static void Main (string[] args)
		{
			var workers = Int32.Parse (args [0]);
			var server = new ServerMain (workers);
#if DEBUG
			Console.WriteLine ("[server] starting");
#endif
			server.Run ().Wait ();
			var served = Requests * workers;
			var elapsed = server.stopwatch.ElapsedMilliseconds;
			Console.WriteLine (
				"[server] workers: {0} elapsed: {1}s served: {2}req mean: {3:F2}req/s",
				workers,
				elapsed / 1000.0,
				served,
				1000.0 * served / elapsed);
		}

		public ServerMain (int workers)
		{
			this.workers = workers;
			workerTasks = new List<Task> ();
			stopwatch = new Stopwatch ();
		}

		private Task Run ()
		{
			TcpListener listener = null;
			IPAddress address = Dns.GetHostEntry ("localhost").AddressList [0];
			try
			{
				listener = new TcpListener (address, Port);
				listener.Start ();
			}
			catch (Exception e)
			{
				Console.Error.WriteLine ("[server] ERROR creating TCP listener: {0}", e);
				System.Environment.Exit (1);
			}

			return Task.Run (() => {
				while (workerTasks.Count < workers) {
#if DEBUG
					Console.WriteLine ("[server] waiting for client");
#endif
					Socket socket = listener.AcceptSocket ();
					var id = workerTasks.Count;
#if DEBUG
					Console.WriteLine ("[server] starting worker #{0}", id);
#endif
					workerTasks.Add (Task.Run (() => {
						Worker (socket, id);
					}));
				}
#if DEBUG
				Console.WriteLine ("[server] waiting for all clients");
#endif
				stopwatch.Start ();
				Task.WhenAll (workerTasks.ToArray ()).ContinueWith (tasks => stopwatch.Stop ()).Wait ();
			});
		}

		private void Worker (Socket socket, int id)
		{
			var buffer = new byte [RequestLength];
			while (true) {
#if DEBUG
				Console.WriteLine ("[server] worker #{0} waiting for request", id);
#endif
				int remaining = RequestLength;
				while (remaining > 0) {
					int received = socket.Receive (buffer, RequestLength - remaining, remaining, SocketFlags.None);
					// A request starting with EOT terminates the worker.
					if (received >= 1 && buffer [0] == EOT)
						goto end;
					remaining -= received;
				}
#if DEBUG
				Console.WriteLine ("[server] worker #{0} received request, sending response", id);
#endif
				// When we’ve received RequestLength bytes, we send ResponseLength bytes.
				var reply = new byte [ResponseLength];
				socket.Send (reply);
#if DEBUG
				Console.WriteLine ("[server] worker #{0} sent response", id);
#endif
			}
			end:
#if DEBUG
			Console.WriteLine ("[server] worker #{0} received terminate request, shutting down worker #{0}", id);
#endif
			socket.Close ();
		}
	}
}
