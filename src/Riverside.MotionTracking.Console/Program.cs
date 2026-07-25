using System;
using System.Threading;
using System.Threading.Tasks;
using Riverside.MotionTracking;

namespace Riverside.MotionTracking.ConsoleTest;

internal static class Program
{
	private const int Port = 12345;

	private static async Task Main()
	{
		Console.Title = "\"Riverside.MotionTracking\" SDK";
		Console.ForegroundColor = ConsoleColor.Cyan;
		Console.WriteLine("================================================");
		Console.WriteLine("  STARTING MOTION TRACKING NETWORK LISTENER...   ");
		Console.WriteLine("================================================");
		Console.ResetColor();

		using var listener = new StreamedListener(Port);
		listener.Start();

		Console.WriteLine($"\n[Listening] Waiting for Pixel Watch data on UDP port {Port}...");
		Console.ForegroundColor = ConsoleColor.DarkGray;
		Console.WriteLine("Tip: If broadcast packets don't arrive, enter your PC's IP in the watch app.");
		Console.WriteLine("     Ensure the watch and PC are on the same network / hotspot.");
		Console.WriteLine($"     Allow 'Riverside.MotionTracking.Console' through Windows Firewall for UDP {Port}.");
		Console.WriteLine("Press [ESC] at any time to exit the test runner.\n");
		Console.ResetColor();

		using var cts = new CancellationTokenSource();
		var displayTask = Task.Run(() => PollTrackingData(listener, cts.Token));

		while (true)
		{
			if (Console.KeyAvailable && Console.ReadKey(intercept: true).Key == ConsoleKey.Escape)
			{
				break;
			}
			await Task.Delay(100);
		}

		Console.ForegroundColor = ConsoleColor.Yellow;
		Console.WriteLine("\n[Shutting Down] Stopping threads and closing sockets...");
		cts.Cancel();
		await displayTask;
		Console.ResetColor();
	}

	private static async Task PollTrackingData(StreamedListener listener, CancellationToken token)
	{
		bool watchDiscovered = false;
		int waitingTicks = 0;
		char[] spinner = ['|', '/', '-', '\\'];

		while (!token.IsCancellationRequested)
		{
			var (x, y, z, w) = listener.GetLatestRotation();
			var endpoint = listener.ConnectedWatchEndPoint;

			if (endpoint != null)
			{
				if (!watchDiscovered)
				{
					watchDiscovered = true;
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine($"\n[SUCCESS] Pixel Watch Discovered!");
					Console.WriteLine($"[Network] Watch endpoint: {endpoint.Address}:{endpoint.Port}\n");
					Console.ResetColor();
				}

				Console.Write($"\r[IMU Quaternion] X: {x:F4} | Y: {y:F4} | Z: {z:F4} | W: {w:F4}        ");
			}
			else
			{
				waitingTicks++;
				if (waitingTicks % 30 == 1)
				{
					Console.ForegroundColor = ConsoleColor.DarkGray;
					Console.Write($"\r[Waiting {spinner[(waitingTicks / 30) % spinner.Length]}] No watch packets yet (UDP {Port})...            ");
					Console.ResetColor();
				}
			}

			await Task.Delay(33, token);
		}
	}
}
