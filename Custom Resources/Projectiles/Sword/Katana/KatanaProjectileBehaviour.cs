using Godot;
using Godot.Collections;
using System;

public partial class KatanaProjectileBehaviour : Area2D
{
	[Export] private AnimationPlayer _animationPlayer;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public KatanaProjectile KatanaData;
	
	private Array<Enemy> _hitEnemies = [];

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_animationPlayer.Play("Slash");
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body is Enemy enemy)
		{
			_hitEnemies.Add(enemy);
			enemy.AddStatusEffectStacks(StatusEffect.Stun, Mathf.Floor((float)((_animationPlayer.CurrentAnimationLength - _animationPlayer.CurrentAnimationPosition) * 10f)));
		}
	}

	public void OnAnimFinished(StringName animName)
	{
		foreach (Enemy enemy in _hitEnemies)
		{
			foreach (TowerEffect effect in KatanaData.Effects)
				effect.ApplyEffect(Stats, enemy);
		}

		if (animName.ToString().Contains("Slash"))
			QueueFree();
	}
}
