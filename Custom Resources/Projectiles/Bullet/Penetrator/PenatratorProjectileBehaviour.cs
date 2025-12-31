using Godot;
using Godot.Collections;
using System;

public partial class PenatratorProjectileBehaviour : CharacterBody2D
{
	[Export] private float _friction;
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public PenatratorProjectile PenatratorData;

	private int _hitCount = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Velocity = -Transform.Y.Normalized() * PenatratorData.FireForce;

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Velocity = Velocity.Lerp(Transform.X * PenatratorData.FireForce / 2f, _friction * (float)delta);

		MoveAndSlide();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			foreach (TowerEffect effect in PenatratorData.Effects)
					effect.ApplyEffect(Stats, (Enemy)body);
			_hitCount++;
			if (_hitCount >= PenatratorData.Pierce)
				QueueFree();
		}
	}
	
	private void OnScreenExited()
    {
		QueueFree();
    }
}
