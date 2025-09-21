 using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyEffectTrigger, EnemyEffect> Effects = [];
	[Export] private Dictionary<EnemyStat, int> _baseEnemyStats = [];
	[Export] private float _acceleration = 5f;
	[Export] private float _deceleration = 10f;
	[Export] private float _offsetMargin = 0.4f;
	[Export] private PackedScene _damageNumberScene;

	public Vector2 TargetPos;
	public Array<Vector2> PathArray = [];
	public Dictionary<EnemyStat, float> CurrentEnemyStats = [];
	public int SpawnedWave;

	private Dictionary<StatusEffect, int> _currentStatusEffects = [];
	private Dictionary<StatusEffect, Timer> _currentStatusEffectTimers = [];
	private Sprite2D _sprite;
	private float _currentHealth;
	private RandomNumberGenerator _rand = new();
	private bool _isDead = false;
	private Timer _reachedBaseFreeTimer = null;

	public override void _Ready()
	{
		Effects.Add(EnemyEffectTrigger.OnDeath, new RewardEffect());

		// Initialises status efects dictionary
		foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)).Cast<StatusEffect>())
		{
			_currentStatusEffects.Add(status, 0);

			Timer timer = new()
			{
				WaitTime = StatusEffectsData.GetStatusEffectDuration(status),
				Autostart = true,
				OneShot = true
			};
			timer.Connect(Timer.SignalName.Timeout, Callable.From(() => AddStatusEffectStacks(status, -1)));
			_currentStatusEffectTimers.Add(status, timer);
			AddChild(timer);
		}

		_sprite = GetChild<Sprite2D>(0);

		foreach ((EnemyStat stat, int value) in _baseEnemyStats)
			CurrentEnemyStats[stat] = value;
		_currentHealth = CurrentEnemyStats[EnemyStat.MaxHealth];

		TriggerEffects(EnemyEffectTrigger.OnSpawn);

		if (Effects.ContainsKey(EnemyEffectTrigger.OnTimer))
		{
			foreach ((EnemyEffectTrigger trigger, EnemyEffect effect) in Effects)
			{
				if (trigger == EnemyEffectTrigger.OnTimer)
				{
					Timer timer = new();
					AddChild(timer);

					timer.WaitTime = effect.EffectInterval;
					timer.Timeout += () => effect.ApplyEffect(this);

					AddChild(timer);
				}
			}
		}

		PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize), (Vector2I)(TargetPos / PathfindingManager.instance.TileSize));
		float offsetMargin = PathfindingManager.instance.TileSize * 0.75f;
		Vector2 offset = new(_rand.RandfRange(-offsetMargin / 2, offsetMargin / 2), _rand.RandfRange(-offsetMargin / 2, offsetMargin / 2));
		for (int i = 1; i < PathArray.Count - 1; i++)
			PathArray[i] += offset;
	}

	public override void _Process(double delta)
	{
		if (PathArray.Count > 1)
		{
			MoveToNextPathPoint((float)delta);

			if (GlobalPosition.DistanceTo(PathArray[0]) <= CurrentEnemyStats[EnemyStat.Speed] / 5f)
				PathArray.RemoveAt(0);
		}
		else if (PathArray.Count == 1)
		{
			// Slow down as reaching goal (looks cool and copying infinitode lmao)
			MoveToNextPathPoint((float)delta, Mathf.Lerp(0.4f, 1f, Mathf.Clamp(GlobalPosition.DistanceTo(PathArray[0]) / 16f, 0f, 1f)));

			if (GlobalPosition.DistanceTo(PathArray[0]) <= 0.5f)
				QueueFree();
		}
		else
		{
			Velocity = Velocity.Lerp(Vector2.Zero, _deceleration * (float)delta);
		}

		MoveAndSlide();
	}

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
	{
		if (!_isDead)
		{
			float damageDealt = amount;
			if (!defenceBreak)
				damageDealt = DamageAfterArmorPierce(damageDealt);

			_currentHealth -= damageDealt;

			InstantiateDamageNumber(damageDealt, damageType);

			TriggerEffects(EnemyEffectTrigger.OnDamage);

			if (_currentHealth <= 0)
			{
				Die();
			}

			return damageDealt;
		}

		return float.NaN;
	}

	public void AddStatusEffectStacks(StatusEffect status, int amount)
	{
		_currentStatusEffects[status] += amount;
		_currentStatusEffects[status] = Mathf.Max(_currentStatusEffects[status], 0);
		_currentStatusEffectTimers[status].Start();
	}

	protected virtual void MoveToNextPathPoint(float delta, float speedMult = 1f)
	{
		Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);

		UpdateSpeedStat();

		Velocity = Velocity.Lerp(dir.Normalized() * CurrentEnemyStats[EnemyStat.Speed] * speedMult, _acceleration * delta);
		_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, dir.Angle(), _acceleration * delta);
	}

	protected virtual void Die()
	{
		_isDead = true;

		TriggerEffects(EnemyEffectTrigger.OnDeath);

		QueueFree();
	}

	protected virtual float UpdateSpeedStat()
	{
		float speed = _baseEnemyStats[EnemyStat.Speed];

		if (_currentStatusEffects[StatusEffect.Chill] > 0)
			speed *= 6.4f / Mathf.Pow(_currentStatusEffects[StatusEffect.Chill] + 3, 2) + 0.35f;

		CurrentEnemyStats[EnemyStat.Speed] = speed;
		return speed;
	}

	protected void TriggerEffects(EnemyEffectTrigger triggerEvent)
	{
		if (Effects != null && Effects.Count != 0)
		{
			foreach ((EnemyEffectTrigger trigger, EnemyEffect effect) in Effects)
			{
				if (trigger == triggerEvent)
				{
					effect.ApplyEffect(this);
				}
			}
		}
	}

	protected void InstantiateDamageNumber(float damageDealt, DamageType damageType)
	{
		DamageNumber damageNumber = _damageNumberScene.Instantiate<DamageNumber>();
		damageNumber.DamageValue = Mathf.Round(damageDealt * 100) / 100;
		damageNumber.DamageTypeDealt = damageType;
		damageNumber.GlobalPosition = GlobalPosition + new Vector2(_rand.RandfRange(-2f, 2f), _rand.RandfRange(-0.5f, 0.5f));
		EnemyManager.instance.EnemyParent.AddChild(damageNumber);
	}

	protected float DamageAfterArmorPierce(float originalDamage)
	{
		if (CurrentEnemyStats.TryGetValue(EnemyStat.Defence, out float defence))
			return originalDamage * Mathf.Pow(0.975f, defence);
		else
			return originalDamage;
	}
}