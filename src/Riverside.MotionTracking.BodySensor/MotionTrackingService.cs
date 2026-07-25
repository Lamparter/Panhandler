using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Hardware;
using Android.OS;
using Android.Runtime;
using Android.Util;

namespace Riverside.MotionTracking.BodySensor;

[Service(Enabled = true, Exported = false, ForegroundServiceType = ForegroundService.TypeDataSync)]
public class MotionTrackingService : Service, ISensorEventListener
{
	public const string ExtraTargetIp = "target_ip";
	private const string Tag = "WatchTracker";
	private const string BroadcastIp = "255.255.255.255";
	private const int TargetPort = 12345;
	private const int NotificationId = 1001;
	private const string ChannelId = "motion_tracking_channel";

	private SensorManager? _sensorManager;
	private Sensor? _rotationVectorSensor;
	private UdpClient? _udpClient;
	private PowerManager.WakeLock? _wakeLock;
	private string _targetIp = BroadcastIp;

	public override IBinder? OnBind(Intent? intent) => null;

	public override void OnCreate()
	{
		base.OnCreate();
		StartForeground(NotificationId, CreateNotification());

		PowerManager? powerManager = (PowerManager?)GetSystemService(PowerService);
		_wakeLock = powerManager?.NewWakeLock(WakeLockFlags.Partial, $"{Tag}::WakeLock");
		_wakeLock?.SetReferenceCounted(false);
		_wakeLock?.Acquire();
	}

	public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
	{
		string? requestedIp = intent?.GetStringExtra(ExtraTargetIp);
		if (!string.IsNullOrWhiteSpace(requestedIp) && IPAddress.TryParse(requestedIp, out _))
		{
			_targetIp = requestedIp!;
		}

		_udpClient = new()
		{
			EnableBroadcast = _targetIp == BroadcastIp
		};

		_sensorManager = (SensorManager?)GetSystemService(SensorService);
		_rotationVectorSensor = _sensorManager?.GetDefaultSensor(SensorType.RotationVector);

		if (_sensorManager != null && _rotationVectorSensor != null)
		{
			_sensorManager.RegisterListener(this, _rotationVectorSensor, SensorDelay.Game);
			Log.Debug(Tag, $"Rotation vector registered: {_rotationVectorSensor.Name}. Target: {_targetIp}:{TargetPort}");
		}
		else
		{
			Log.Error(Tag, "No rotation vector sensor available on this device");
			StopSelf();
		}

		return StartCommandResult.Sticky;
	}

	public void OnSensorChanged(SensorEvent? e)
	{
		if (e?.Sensor?.Type != SensorType.RotationVector || e.Values == null)
			return;

		float x = e.Values[0];
		float y = e.Values[1];
		float z = e.Values[2];
		float w = e.Values.Count > 3 ? e.Values[3] : 0.0f;

		string payload = $"{x:F4},{y:F4},{z:F4},{w:F4}";
		Log.Debug(Tag, $"Sending payload to {_targetIp}:{TargetPort}: {payload}");
		byte[] bytes = Encoding.UTF8.GetBytes(payload);

		try
		{
			_udpClient?.Send(bytes, bytes.Length, _targetIp, TargetPort);
		}
		catch (Exception ex)
		{
			Log.Error(Tag, $"UDP send failed: {ex.Message}");
		}
	}

	public void OnAccuracyChanged(Sensor? sensor, SensorStatus accuracy) { }

	public override void OnDestroy()
	{
		_sensorManager?.UnregisterListener(this);
		_udpClient?.Dispose();
		StopForeground(StopForegroundFlags.Remove);

		if (_wakeLock?.IsHeld == true)
			_wakeLock.Release();
		_wakeLock?.Dispose();

		base.OnDestroy();
	}

	private Notification CreateNotification()
	{
		if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
		{
			var channel = new NotificationChannel(ChannelId, "Motion Tracking", NotificationImportance.Low);
			NotificationManager? notificationManager = (NotificationManager?)GetSystemService(NotificationService);
			notificationManager?.CreateNotificationChannel(channel);
		}

		return new Notification.Builder(this, ChannelId)
			.SetContentTitle("Watch Tracker")
			.SetContentText($"Streaming rotation to {_targetIp}:{TargetPort}")
			.SetSmallIcon(Android.Resource.Drawable.StatNotifySync)
			.SetOngoing(true)
			.Build();
	}
}
