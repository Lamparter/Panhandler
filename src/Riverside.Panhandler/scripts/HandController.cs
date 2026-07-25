using Godot;

public partial class HandController : CharacterBody3D
{
	public const float Speed = 0.05f;
	private Vector3 _rotVelocity;

	private float _yaw;
	private float _pitch;
	private float _roll;

	public override void _PhysicsProcess(double delta)
	{
		// Get the input direction and handle the movement/deceleration.
		// As good practice, you should replace UI actions with custom gameplay actions.
		Basis = Basis.Identity;
		Vector2 inputDir = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
		Vector3 direction = (Transform.Basis * new Vector3(0, -inputDir.X, -inputDir.Y)).Normalized();
		if (direction != Vector3.Zero)
		{
			_rotVelocity.X = direction.X * Speed;
			_rotVelocity.Y = direction.Y * Speed;
			_rotVelocity.Z = direction.Z * Speed;
		}
		else
		{
			_rotVelocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
			_rotVelocity.Y = Mathf.MoveToward(Velocity.Y, 0, Speed);
			_rotVelocity.Z = Mathf.MoveToward(Velocity.Z, 0, Speed);
		}

		_yaw += _rotVelocity.Y;
		_pitch += _rotVelocity.Z;

		Basis = Basis.Rotated(Basis.X, _pitch);
		Basis = Basis.Rotated(Vector3.Up, _yaw);
		Basis = Basis.Orthonormalized();

		MoveAndSlide();
	}

	private void ApplyRotation()
	{
		// reset rotation
		Transform3D transform = Transform;
		transform.Basis = Basis.Identity;
		Transform = transform;

		RotateObjectLocal(Vector3.Up, _yaw); // first rotate about Y
		RotateObjectLocal(Vector3.Right, _pitch); // then rotate about X
	}
}
