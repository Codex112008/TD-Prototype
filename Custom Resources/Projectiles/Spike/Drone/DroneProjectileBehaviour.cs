using Godot;
using Godot.Collections;
using System;

public partial class DroneProjectileBehaviour : PathfindingEntity
{
	[Export] private Sprite2D _sprite;
	[Export] private float _friction;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public DroneProjectile DroneData;
	
	private float _currentHealth;
	private bool _landedOnTrack = false;
	private bool _showMaxHp = true;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_currentHealth = Stats[TowerStat.Damage];

		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(3).Visible = true;
			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + Stats[TowerStat.Damage];
			GetChild<RichTextLabel>(3).Rotation = -Mathf.Pi / 2f;
		}

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;

		_acceleration = DroneData.SummonAcceleration;
		// Not calling base ready function as path should be calculated after landing on track (techy yippee)
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (_landedOnTrack)
		{
			if (PathArray == null)
			{
				RandomNumberGenerator rand = new();
				PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize), (Vector2I)(EnemyManager.instance.SpawnPoints[rand.RandiRange(0, EnemyManager.instance.SpawnPoints.Count - 1)] / PathfindingManager.instance.TileSize));
				float offsetMargin = PathfindingManager.instance.TileSize * 0.75f;
				Vector2 offset = new(rand.RandfRange(-offsetMargin / 2f, offsetMargin / 2f), rand.RandfRange(-offsetMargin / 2f, offsetMargin / 20f));
				for (int i = 1; i < PathArray.Count - 1; i++)
					PathArray[i] += offset;
			}

			_speed = DroneData.SummonSpeed;
			base._PhysicsProcess(delta);
		}
		else
		{
			if (GlobalPosition.DistanceTo(TargetPos) < 8f)
				Velocity = Velocity.Lerp(Vector2.Zero, _friction * (float)delta);
			else
				Velocity = -Transform.Y.Normalized() * DroneData.ProjectileSpeed;

			if (Velocity.IsZeroApprox())
				_landedOnTrack = true;;

			MoveAndSlide();
		}
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			Enemy enemy = (Enemy)body;
			Dictionary<TowerStat, float> statsToUse = new(Stats);
			float statMultiplier = _currentHealth / Stats[TowerStat.Damage];

			// If enemy has les hp than spike then weaken effect to do just enough to enemy
			float enemyHealth = enemy.GetCurrentHealth();
			if (enemyHealth < _currentHealth)
			{
				statMultiplier = enemyHealth / Stats[TowerStat.Damage];
			}

			foreach (TowerStat stat in statsToUse.Keys)
				statsToUse[stat] *= statMultiplier;

			_currentHealth = Mathf.Max(_currentHealth - enemy.GetCurrentHealth(), 0);

			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + Stats[TowerStat.Damage];

			foreach (TowerEffect effect in DroneData.Effects)
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
