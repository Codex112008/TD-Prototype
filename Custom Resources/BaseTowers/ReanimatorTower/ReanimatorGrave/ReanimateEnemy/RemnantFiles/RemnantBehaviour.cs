using Godot;
using Godot.Collections;
using System;

public partial class RemnantBehaviour : CharacterBody2D
{
	[Export] public Sprite2D Sprite;
	[Export] public float Acceleration = 4f;
	
	public Tower Tower;
	public float MaxHealth;
	public float Speed;
	public Array<Vector2> PathArray = null;
	public Vector2 TargetPos;
	
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
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

		if (PathArray.Count == 0)
			QueueFree();
		else
		{
			Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);
			Velocity = Velocity.Lerp(dir.Normalized() * Speed * 0.5f, Acceleration *  (float)delta);
			Sprite.Rotation = Mathf.LerpAngle(Sprite.Rotation, dir.Angle(), Acceleration * (float)delta);
			Rotation = Mathf.LerpAngle(Rotation, Mathf.Pi / 2f, Acceleration * (float)delta);

			if (GlobalPosition.DistanceTo(PathArray[0]) <= Speed / 5f)
				PathArray.RemoveAt(0);
		}

		MoveAndSlide();
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
