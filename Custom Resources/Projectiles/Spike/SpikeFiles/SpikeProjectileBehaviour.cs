using Godot;
using Godot.Collections;
using System;

public partial class SpikeProjectileBehaviour : CharacterBody2D
{
	[Export] private float _friction;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public SpikeProjectile SpikeData;
	
	private float _currentHealth;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentHealth = Stats[TowerStat.Damage];

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!PathfindingManager.instance.IsTileAtGlobalPosSolid(GlobalPosition))
			Velocity = Velocity.Lerp(Vector2.Zero, _friction * (float)delta);
		else
			Velocity = -Transform.Y.Normalized() * SpikeData.Speed;

		MoveAndSlide();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			Enemy enemy = (Enemy)body;
			Dictionary<TowerStat, float> statsToUse = new(Stats);
			float statMultiplier = _currentHealth / Stats[TowerStat.Damage] ;

			// If enemy has les hp than spike then weaken effect to do just enough to enemy
			float enemyHealth = enemy.GetCurrentHealth();
			if (enemyHealth < _currentHealth)
			{
				statMultiplier = enemyHealth / Stats[TowerStat.Damage];
			}

			foreach (TowerStat stat in statsToUse.Keys)
				statsToUse[stat] *= statMultiplier;

			_currentHealth -= enemy.GetCurrentHealth();

			foreach (TowerEffect effect in SpikeData.Effects)
				effect.ApplyEffect(statsToUse, enemy);
			
			if (_currentHealth <= 0f)
				QueueFree();
		}
	}

	private void OnScreenExited()
    {
		QueueFree();
    }
}
