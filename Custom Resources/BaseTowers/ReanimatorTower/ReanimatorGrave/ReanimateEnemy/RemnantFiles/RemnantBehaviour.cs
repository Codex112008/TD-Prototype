using Godot;
using Godot.Collections;
using System;

public partial class RemnantBehaviour : PathfindingEntity
{
	[Export] public float Acceleration = 4f;
	
	public Tower Tower;
	public float MaxHealth;
	public float Speed;
	
	private float _currentHealth;
	private bool _showMaxHp = true;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentHealth = MaxHealth;

		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(3).Visible = true;
			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + MaxHealth;
			GetChild<RichTextLabel>(3).Rotation = -Mathf.Pi / 2f;
		}

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;

		_acceleration = Acceleration;
		base._Ready();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		_speed = Speed;
		base._PhysicsProcess(delta);
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy") && body is not ReanimatorGraveBehaviour)
		{
			Enemy enemy = (Enemy)body;
			Dictionary<TowerStat, float> statsToUse = Tower.GetFinalTowerStats();
			statsToUse[TowerStat.Damage] = MaxHealth;
			statsToUse[TowerStat.FireRate] = Speed;

			float statMultiplier = _currentHealth / MaxHealth;

			// If enemy has les hp than spike then weaken effect to do just enough to enemy
			float enemyHealth = enemy.GetCurrentHealth();
			if (enemyHealth < _currentHealth)
			{
				statMultiplier = enemyHealth / MaxHealth;
			}

			foreach (TowerStat stat in statsToUse.Keys)
				statsToUse[stat] *= statMultiplier;

			_currentHealth = Mathf.Max(_currentHealth - enemy.GetCurrentHealth(), 0);

			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + MaxHealth;

			enemy.RegisterDeathSignal = false;

			foreach (TowerEffect effect in Tower.Projectile.Effects)
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
