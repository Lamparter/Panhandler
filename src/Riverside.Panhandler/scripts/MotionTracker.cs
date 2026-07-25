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

		Quaternion rotation = new(x, y, z, w);
		EmitSignal(SignalName.QuaternionUpdated, rotation);
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
