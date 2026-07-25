using System;
using System.Net.Sockets;
using System.Text;
using Android.App;
using Android.Content;
using Android.Hardware;
using Android.OS;
using Android.Runtime;

namespace Riverside.MotionTracking.BodySensor;

public class MotionTrackingService : Service, ISensorEventListener
{
	// change later
	private const string TargetIp = "";
	private const int TargetPort = 12345;

	private SensorManager? _sensorManager;
	private Sensor? _rotationVectorSensor;
	private UdpClient? _udpClient;

	public override IBinder? OnBind(Intent? intent) => null;

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		_sensorManager = (SensorManager?)GetSystemService(SensorService);
		_rotationVectorSensor = _sensorManager?.GetDefaultSensor(SensorType.RotationVector);
		_udpClient = new();

		if (_sensorManager != null && _rotationVectorSensor != null)
			_sensorManager.RegisterListener(this, _rotationVectorSensor, SensorDelay.Game); // ~ 50 Hz

		return StartCommandResult.Sticky;
	}

	public void OnSensorChanged(SensorEvent? e)
	{
		if (e?.Sensor?.Type != SensorType.RotationVector || e.Values == null)
			return;

		float x = e.Values[0];
		float y = e.Values[1];
		float z = e.Values[2];
		float w = e.Values.Count > 3 ? e.Values[3] : 0.0f; // ?

		string payload = $"{x:F4},{y:F4},{z:F4},{w:F4}";
		byte[] bytes = Encoding.UTF8.GetBytes(payload);

		try
		{
			_udpClient?.SendAsync(bytes, bytes.Length, TargetIp, TargetPort);
		}
		catch (SocketException) { }
	}

	public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

	public override void OnDestroy()
	{
		_sensorManager?.UnregisterListener(this);
		_udpClient?.Dispose();
		base.OnDestroy();
	}
}