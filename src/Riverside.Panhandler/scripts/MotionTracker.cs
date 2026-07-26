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
	public Quaternion CalibrationOffset { get; set; } = Quaternion.Identity;

	// Fixed adjustment around the watch's forward axis. Increase/decrease in
	// 90-degree steps if the saucepan appears rolled relative to the hand.
	[Export(PropertyHint.Range, "-180,180,90")]
	public float PanRollOffsetDegrees { get; set; } = 0f;

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

		Quaternion watchRotation = new(-y, z, -x, w);
		if (watchRotation.LengthSquared() < 0.0001f)
			return;

		Quaternion godotRotation = RemapWatchToGodot(watchRotation.Normalized());
		godotRotation = CalibrationOffset * godotRotation;

		if (!Mathf.IsZeroApprox(PanRollOffsetDegrees))
		{
			Quaternion roll = Quaternion.FromEuler(new Vector3(0, 0, Mathf.DegToRad(PanRollOffsetDegrees)));
			godotRotation = roll * godotRotation;
		}

		EmitSignal(SignalName.QuaternionUpdated, godotRotation);
	}

	/// <summary>
	/// Converts the watch rotation from Android world space into Godot world space.
	///
	/// Android world axes: X = east, Y = north, Z = up.
	/// Godot world axes:   X = right, Y = up, Z = back (so forward is -Z).
	///
	/// We build the watch's basis directly from the quaternion in Android space,
	/// then multiply by the matrix that converts Android space to Godot space.
	/// No individual watch-axis tuning is needed because the quaternion already
	/// encodes which way the crown, screen, and back of the watch are pointing.
	/// </summary>
	private Quaternion RemapWatchToGodot(Quaternion watchRotation)
	{
		// Columns are Android X, Y, Z expressed in Godot coordinates:
		//   east  -> right
		//   north -> forward (-Z)
		//   up    -> up
		Basis androidToGodot = new(Vector3.Right, Vector3.Forward, Vector3.Up);

		// watchRotation is defined relative to Android world space.
		// new Basis(watchRotation) maps watch-local vectors to Android-world vectors.
		// Multiplying by androidToGodot expresses that same basis in Godot world.
		Basis watchBasisInGodot = new Basis(watchRotation);
		return watchBasisInGodot.GetRotationQuaternion().Normalized();
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
