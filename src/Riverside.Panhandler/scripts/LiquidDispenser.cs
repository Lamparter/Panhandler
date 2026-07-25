using System;
using Godot;

public partial class LiquidDispenser : Node3D
{
	private GpuParticles3D _milkParticles;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready() { 
		_milkParticles = GetNode<GpuParticles3D>("LiquidEmitter");
	}

	public void _OnBodyEntered(Node3D body)
	{
		if (body.Name == "HandBody")
		{
			_milkParticles.Emitting = true;
		}
	}

	public void _OnBodyExited(Node3D body)
	{
		if (body.Name == "HandBody")
		{
			_milkParticles.Emitting = false;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta) { }
}
