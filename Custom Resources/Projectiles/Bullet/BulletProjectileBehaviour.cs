using Godot;
using Godot.Collections;
using System;

public partial class BulletProjectileBehaviour : CharacterBody2D
{
	[Export] private float _friction;
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public BulletProjectile BulletData;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Velocity = -Transform.Y * BulletData.fireForce;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		Velocity = Velocity.Lerp(Transform.X * BulletData.fireForce / 2f, _friction * (float)delta);

		MoveAndSlide();

		for (int i = 0; i < GetSlideCollisionCount(); i++)
		{
			Node hitNode = (Node)GetSlideCollision(i).GetCollider();
			if (hitNode.IsInGroup("Enemy"))
			{
				foreach (TowerEffect effect in BulletData.Effects)
					effect.ApplyEffect(Stats, (Enemy)hitNode);
				QueueFree();
				break;
			}
		}
	}
}
