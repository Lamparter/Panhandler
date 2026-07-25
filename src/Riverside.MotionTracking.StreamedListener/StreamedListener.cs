using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Riverside.MotionTracking;

public class StreamedListener : IDisposable
{
	private readonly int _listenPort;
	private UdpClient? _udpListener;
	private CancellationTokenSource? _cts;
	private bool _isDisposed;

	private readonly object _lock = new();
	private float _x, _y, _z, _w;
	private IPEndPoint? _connectedWatchEndPoint;

	public IPEndPoint? ConnectedWatchEndPoint
	{
		get
		{
			lock (_lock)
			{
				return _connectedWatchEndPoint;
			}
		}
	}

	public StreamedListener(int port = 12345)
	{
		_listenPort = port;
	}

	public void Start()
	{
		if (_udpListener != null) return;

		_udpListener = new()
		{
			ExclusiveAddressUse = false
		};

		_udpListener.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
		_udpListener.Client.Bind(new IPEndPoint(IPAddress.Any, _listenPort));

		_cts = new();

		Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
	}

	private async Task ListenAsync(CancellationToken cancellationToken)
	{
		IPEndPoint localEndPoint = (IPEndPoint?)_udpListener?.Client.LocalEndPoint ?? new IPEndPoint(IPAddress.Any, _listenPort);
		Console.WriteLine($"[Listener] Bound to {localEndPoint}. Waiting for watch packets...");

		while (!cancellationToken.IsCancellationRequested && _udpListener != null)
		{
			try
			{
				UdpReceiveResult result = await _udpListener.ReceiveAsync(cancellationToken);
				string payload = Encoding.UTF8.GetString(result.Buffer);
				Console.WriteLine($"[Raw] {payload} <- {result.RemoteEndPoint}");

				if (TryParsePayload(payload, out float x, out float y, out float z, out float w))
				{
					lock (_lock)
					{
						_x = x;
						_y = y;
						_z = z;
						_w = w;

						_connectedWatchEndPoint = result.RemoteEndPoint;
					}
				}
				else
				{
					Console.WriteLine($"[Parse] Ignored malformed payload: {payload}");
				}
			}
			catch (OperationCanceledException) { break; }
			catch (Exception ex)
			{
				Console.WriteLine($"[Listener Error] {ex.GetType().Name}: {ex.Message}");
			}
		}
	}

	private static bool TryParsePayload(string payload, out float x, out float y, out float z, out float w)
	{
		x = y = z = w = 0;

		string[] tokens = payload.Split(',');
		if (tokens.Length != 4)
			return false;

		return float.TryParse(tokens[0], out x) &&
			   float.TryParse(tokens[1], out y) &&
			   float.TryParse(tokens[2], out z) &&
			   float.TryParse(tokens[3], out w);
	}

	public (float x, float y, float z, float w) GetLatestRotation()
	{
		lock (_lock)
		{
			return (_x, _y, _z, _w);
		}
	}

	public void Stop()
	{
		_cts?.Cancel();
		_udpListener?.Close();

		_cts = null;
		_udpListener = null;
	}

	public void Dispose()
	{
		if (_isDisposed) return;
		Stop();
		_cts?.Dispose();
		_isDisposed = true;
		GC.SuppressFinalize(this);
	}
}
