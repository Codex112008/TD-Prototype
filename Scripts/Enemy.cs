using Godot;
using Godot.Collections;
using System;

public partial class Enemy : CharacterBody2D
{
	[Export] private float maxHealth = 5f;
	[Export] private float speed;
	public Array<Vector2> waypoints;
	private float health;
	private int currentWaypoint = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		health = maxHealth;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2 dir = Position.DirectionTo(waypoints[currentWaypoint]).Normalized();

		Velocity = Velocity.Lerp(dir * speed, 5f * (float)delta);
		if (Position.DistanceTo(waypoints[currentWaypoint]) <= 0.1f)
		{
			currentWaypoint++;
		}

		MoveAndSlide();
	}

	public void TakeDamage(float amount)
	{
		health -= amount;
		if (health <= 0)
			QueueFree();
	}
}
