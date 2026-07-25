using Godot;

public partial class HandController : CharacterBody3D
{
	public const float RotationSpeed = 0.001f;
	public const float MovementSpeed = 5f;
	public bool AcceptingMouseInputs = false;

	private Vector3 _rotVelocity;

	private Vector2 _mouseMotion;
	private float _yaw;
	private float _pitch;
	private float _roll;

	private Quaternion _targetQuaternion = Quaternion.Identity;
	private bool _useQuaternionRotation = false;

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseMotion motion)
		{
			Vector2 relative = motion.Relative;
			if (relative.Length() <= 100)
			{
				AcceptingMouseInputs = true;
			}
			if (!AcceptingMouseInputs)
			{
				return;
			}
			_mouseMotion = relative;
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		Vector3 velocity = Velocity;
		Vector2 moveDir = Input.GetVector(
			"panhandler_move_left",
			"panhandler_move_right",
			"panhandler_move_forward",
			"panhandler_move_backward"
		);

		if (moveDir != Vector2.Zero)
		{
			velocity.X = moveDir.X * MovementSpeed;
			velocity.Z = moveDir.Y * MovementSpeed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, MovementSpeed);
			velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MovementSpeed);
		}

		if (_useQuaternionRotation)
		{
			Basis = new Basis(_targetQuaternion).Orthonormalized();
		}
		else
		{
			_yaw -= _mouseMotion.X * RotationSpeed;
			_pitch -= _mouseMotion.Y * RotationSpeed;
			_mouseMotion = Vector2.Zero;

			Basis = Basis.Identity;
			Basis = Basis.Rotated(Basis.X, _pitch);
			Basis = Basis.Rotated(Vector3.Up, _yaw);
			Basis = Basis.Orthonormalized();
		}

		Velocity = velocity;
		MoveAndSlide();
	}

	public void OnMotionTrackerUpdate(Quaternion quaternion)
	{
		if (quaternion.LengthSquared() < 0.0001f)
			return;

		_targetQuaternion = quaternion.Normalized();
		_useQuaternionRotation = true;
		GD.Print($"Hand rotation applied: {_targetQuaternion} -> Basis: {new Basis(_targetQuaternion)}");
	}
}
