using System;
using Godot;

public partial class PancakeDispenser : Node3D
{
	private PackedScene _pancake;
	private bool _panInArea = false;

	public void _OnBodyEntered(Node3D body)
	{
			Node instance = _pancake.Instantiate();
			GetNode<Node3D>("dispensed").AddChild(instance);
	}


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_pancake = GD.Load<PackedScene>("res://scenes/pancake.tscn");
	}

}
