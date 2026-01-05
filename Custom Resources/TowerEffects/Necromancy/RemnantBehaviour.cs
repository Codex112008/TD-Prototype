using Godot;
using Godot.Collections;
using System;

public partial class RemnantBehaviour : CharacterBody2D
{
	[Export] public Sprite2D Sprite;
	
	public NecromancyEffect NecromancyData;
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
			Velocity = Velocity.Lerp(dir.Normalized() * Speed, NecromancyData.RemnantAcceleration *  (float)delta);
			Sprite.Rotation = Mathf.LerpAngle(Sprite.Rotation, dir.Angle(), NecromancyData.RemnantAcceleration * (float)delta);
			Rotation = Mathf.LerpAngle(Rotation, Mathf.Pi / 2f, NecromancyData.RemnantAcceleration * (float)delta);

			if (GlobalPosition.DistanceTo(PathArray[0]) <= Speed / 5f)
				PathArray.RemoveAt(0);
		}

		MoveAndSlide();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			Enemy enemy = (Enemy)body;
			float damageToDeal = _currentHealth;
			float damageMultiplier = _currentHealth / MaxHealth;

			// If enemy has les hp then weaken damage to do just enough to enemy
			float enemyHealth = enemy.GetCurrentHealth();
			if (enemyHealth < _currentHealth)
			{
				damageMultiplier = enemyHealth / MaxHealth;
			}

			damageToDeal *= damageMultiplier;

			_currentHealth = Mathf.Max(_currentHealth - enemy.GetCurrentHealth(), 0);

			GetChild<RichTextLabel>(3).Text = _currentHealth.ToString() + '/' + MaxHealth;

			NecromancyData.RemnantDamageEffect.ApplyEffect(new(){{TowerStat.Damage, damageToDeal}}, enemy);
			
			if (_currentHealth <= 0f)
				QueueFree();
		}
	}

	private void OnScreenExited()
    {
		QueueFree();
    }
}
