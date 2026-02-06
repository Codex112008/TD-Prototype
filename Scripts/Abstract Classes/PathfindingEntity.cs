using Godot;
using Godot.Collections;
using System;

public abstract partial class PathfindingEntity : CharacterBody2D
{
	[Export] public Sprite2D Sprite = null;
	[Export] protected float _acceleration = 5f;
	[Export] private float _deceleration = 10f;
	[Export] private float _offsetMargin = 0.4f;

	public Vector2 TargetPos;
	public Array<Vector2> PathArray = [];

	protected RandomNumberGenerator _rand = new();
	protected float _speed;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize), (Vector2I)(TargetPos / PathfindingManager.instance.TileSize));
		float offsetMargin = PathfindingManager.instance.TileSize * 0.75f;
		Vector2 offset = new(_rand.RandfRange(-offsetMargin / 2f, offsetMargin / 2f), _rand.RandfRange(-offsetMargin / 2f, offsetMargin / 2f));
		for (int i = 1; i < PathArray.Count - 1; i++)
			PathArray[i] += offset;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (PathArray.Count > 1)
		{
			MoveToNextPathPoint((float)delta);

			if (GlobalPosition.DistanceTo(PathArray[0]) <= _speed / 5f)
				PathArray.RemoveAt(0);
		}
		else if (PathArray.Count == 1)
		{
			// Slow down as reaching goal (looks cool and copying infinitode lmao)
			MoveToNextPathPoint((float)delta, Mathf.Lerp(0.4f, 1f, Mathf.Clamp(GlobalPosition.DistanceTo(PathArray[0]) / 16f, 0f, 1f)));

			if (GlobalPosition.DistanceTo(PathArray[0]) <= 0.5f)
			{
				ReachedPathEnd();
			}
		}
		else
		{
			Velocity = Velocity.Lerp(Vector2.Zero, _deceleration * (float)delta);
		}

		if (IsInsideTree())
			MoveAndSlide();
	}

	protected virtual void MoveToNextPathPoint(float delta, float speedMult = 1f)
	{
		Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);

		Velocity = Velocity.Lerp(dir.Normalized() * _speed * speedMult, _acceleration * delta);
		Sprite.Rotation = Mathf.LerpAngle(Sprite.Rotation, dir.Angle(), _acceleration * delta);
	}

	protected virtual void ReachedPathEnd()
	{
		QueueFree();
	}
}
