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
	private float _qx, _qy, _qz, _qw;

	public StreamedListener(int port)
	{
		_listenPort = port;
	}

	public void Start()
	{
		if (_udpListener != null) return;

		_udpListener = new(_listenPort);
		_cts = new();

		Task.Run(() => ListenAsync(_cts.Token), _cts.Token);
	}

	private async Task ListenAsync(CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && _udpListener != null)
		{
			try
			{
				UdpReceiveResult result = await _udpListener.ReceiveAsync(cancellationToken);
				string payload = Encoding.UTF8.GetString(result.Buffer);

				string[] tokens = payload.Split(',');
				if (tokens.Length >= 4 &&
					float.TryParse(tokens[0], out float x) &&
					float.TryParse(tokens[1], out float y) &&
					float.TryParse(tokens[2], out float z) &&
					float.TryParse(tokens[3], out float w))
				{
					lock (_lock)
					{
						_qx = x;
						_qy = y;
						_qz = z;
						_qw = w;
					}
				}
			}
			catch (OperationCanceledException) { break; }
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}
		}
	}

	public (float x, float y, float z, float w) GetLatestRotation()
	{
		lock (_lock)
		{
			return (_qx, _qy, _qz, _qw);
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
