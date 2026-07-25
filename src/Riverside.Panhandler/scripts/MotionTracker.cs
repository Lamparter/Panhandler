using System;
using Godot;
using Riverside.MotionTracking;

namespace Riverside.Panhandler;

[GlobalClass]
public partial class MotionTracker : Node
{
	private const int DefaultListenPort = 12345;

	[Signal]
	public delegate void QuaternionUpdatedEventHandler(Quaternion quaternion);

	[Export(PropertyHint.Range, "1024,65535,1")]
	public int ListenPort { get; set; } = DefaultListenPort;

	[Export]
	public bool AutoStart { get; set; } = true;

	[Export]
	public Vector3 WatchXAxisInGodot { get; set; } = Vector3.Right;

	[Export]
	public Vector3 WatchYAxisInGodot { get; set; } = Vector3.Forward;

	[Export]
	public Vector3 WatchZAxisInGodot { get; set; } = Vector3.Up;

	[Export]
	public Quaternion CalibrationOffset { get; set; } = Quaternion.Identity;

	private StreamedListener? _listener;

	public override void _Ready()
	{
		if (AutoStart)
		{
			Start();
		}
	}

	public override void _Process(double delta)
	{
		if (_listener == null)
			return;

		(float x, float y, float z, float w) = _listener.GetLatestRotation();

		Quaternion watchRotation = new(x, y, z, w);
		if (watchRotation.LengthSquared() < 0.0001f)
			return;

		Quaternion godotRotation = RemapWatchToGodot(watchRotation.Normalized());
		godotRotation = CalibrationOffset * godotRotation;

		EmitSignal(SignalName.QuaternionUpdated, godotRotation);
	}

	private Quaternion RemapWatchToGodot(Quaternion watchRotation)
	{
		Basis watchBasisInGodot = new(WatchXAxisInGodot, WatchYAxisInGodot, WatchZAxisInGodot);
		watchBasisInGodot = watchBasisInGodot.Orthonormalized();

		if (Mathf.IsZeroApprox(watchBasisInGodot.Determinant()))
		{
			GD.PushWarning("MotionTracker: watch axis basis is degenerate; skipping remap.");
			return watchRotation;
		}

		Quaternion remap = watchBasisInGodot.GetRotationQuaternion();
		return remap * watchRotation * remap.Inverse();
	}

	public void Calibrate()
	{
		if (_listener == null)
			return;

		(float x, float y, float z, float w) = _listener.GetLatestRotation();
		Quaternion watchRotation = new(x, y, z, w);
		if (watchRotation.LengthSquared() < 0.0001f)
			return;

		CalibrationOffset = RemapWatchToGodot(watchRotation.Normalized()).Inverse();
		GD.Print($"MotionTracker: calibration offset set to {CalibrationOffset}");
	}

	public override void _ExitTree()
	{
		Stop();
		base._ExitTree();
	}

	public void Start()
	{
		if (_listener != null)
			return;

		GD.Print($"WatchMotionTracker: starting UDP listener on port {ListenPort}...");

		_listener = new StreamedListener(ListenPort);
		_listener.Start();
	}

	public void Stop()
	{
		_listener?.Dispose();
		_listener = null;
	}
}
