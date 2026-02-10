using Godot;
using Godot.Collections;
using System;

public partial class SpikeProjectileBehaviour : CharacterBody2D
{
	[Export] private Sprite2D _sprite;
	[Export] private float _friction;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public SpikeProjectile SpikeData;
	public Vector2 TargetPos;
	
	private float _currentHealth;
	private float _targetRot;
	private bool _showMaxHp = true;
	private bool _isDead = false;
	private bool _reachedPos = false;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentHealth = Stats[TowerStat.Damage];

		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(3).Visible = true;
			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + Stats[TowerStat.Damage];
			GetChild<RichTextLabel>(3).Rotation = -Rotation;
		}

		RandomNumberGenerator rand = new();
		_targetRot = rand.RandfRange(Mathf.Pi / 2f, 3f * Mathf.Pi/ 2f);

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (GlobalPosition.DistanceTo(TargetPos) < 8f)
			_reachedPos = true;

		if (_reachedPos && !PathfindingManager.instance.IsTileAtGlobalPosSolid(GlobalPosition))
			Velocity = Velocity.Lerp(Vector2.Zero, _friction * (float)delta);
		else
			Velocity = -Transform.Y.Normalized() * SpikeData.ProjectileSpeed;

		if (!Velocity.IsZeroApprox())
			_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, _targetRot, Velocity.Length() / 10f * (float)delta);

		MoveAndSlide();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (!_isDead && body.IsInGroup("Enemy"))
		{
			Enemy enemy = (Enemy)body;
			Dictionary<TowerStat, float> statsToUse = new(Stats);
			float statMultiplier = _currentHealth / Stats[TowerStat.Damage] ;

			// If enemy has les hp than spike then weaken effect to do just enough to enemy
			float enemyHealth = enemy.GetCurrentHealth();
			if (enemy.DamageBeforeArmorPierce(enemyHealth) < _currentHealth)
			{
				statMultiplier = enemy.DamageBeforeArmorPierce(enemyHealth) / Stats[TowerStat.Damage];
			}

			foreach (TowerStat stat in statsToUse.Keys)
				statsToUse[stat] *= statMultiplier;

			_currentHealth = Mathf.Max(_currentHealth - enemy.DamageBeforeArmorPierce(enemyHealth), 0);

			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + Stats[TowerStat.Damage];

			foreach (TowerEffect effect in SpikeData.Effects)
				effect.ApplyEffect(statsToUse, enemy);
			
			if (_currentHealth <= 0f)
			{
				_isDead = true;
				QueueFree();
			}
		}
	}

	private void OnScreenExited()
    {
		QueueFree();
    }
}
