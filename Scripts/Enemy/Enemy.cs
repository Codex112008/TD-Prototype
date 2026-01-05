using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyEffectTrigger, Array<EnemyEffect>> Effects = [];
	[Export] private Dictionary<EnemyStat, int> _baseEnemyStats = new()
	{
		{EnemyStat.Damage, 1},
		{EnemyStat.DeathReward, 10},
		{EnemyStat.Defence, 1},
		{EnemyStat.MaxHealth, 8},
		{EnemyStat.Speed, 30}
	};
	[Export] private float _acceleration = 5f;
	[Export] private float _deceleration = 10f;
	[Export] private float _offsetMargin = 0.4f;
	[Export] private PackedScene _damageNumberScene;

	public Vector2 TargetPos;
	public Array<Vector2> PathArray = [];
	public Dictionary<EnemyStat, float> CurrentEnemyStats = [];
	public Array<Timer> TimerEffectTimers = [];
	public int SpawnedWave = -1;
	public Sprite2D Sprite;

	protected Dictionary<StatusEffect, float> _currentStatusEffects = [];
	protected Dictionary<StatusEffect, Timer> _currentStatusEffectDecayTimers = [];
	private Dictionary<StatusEffect, Timer> _currentStatusEffectTickTimers = [];
	private float _currentHealth;
	private RandomNumberGenerator _rand = new();
	private bool _isDead = false;
	private Timer _reachedBaseFreeTimer = null;
	private bool _showMaxHp = true;

	public override void _Ready()
	{
		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(2).Visible = true;
			GetChild<RichTextLabel>(2).Text = _baseEnemyStats[EnemyStat.MaxHealth].ToString() + '/' + _baseEnemyStats[EnemyStat.MaxHealth];
		}		

		// Initialises status efects dictionary
		foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)).Cast<StatusEffect>())
		{
			_currentStatusEffects.Add(status, 0f);

			if (StatusEffectsData.DoesStatusEfectDecay(status))
			{
				Timer timer = new()
				{
					WaitTime = StatusEffectsData.GetStatusDecayCooldown(status),
					OneShot = true
				};
				timer.Connect(Timer.SignalName.Timeout, Callable.From(() => AddStatusEffectStacks(status, -1, true)));
				_currentStatusEffectDecayTimers.Add(status, timer);
				AddChild(timer);
			}

			if (StatusEffectsData.IsStatusEfectTicking(status))
			{
				Timer timer = new()
				{
					WaitTime = StatusEffectsData.GetBaseStatusEffectTickInterval(status),
					OneShot = true
				};
				timer.Connect(Timer.SignalName.Timeout, Callable.From(() => TickStatusEffect(status)));
				_currentStatusEffectTickTimers.Add(status, timer);
				AddChild(timer);
			}
		}

		Sprite = GetChild<Sprite2D>(0);

		foreach ((EnemyStat stat, int value) in _baseEnemyStats)
			CurrentEnemyStats[stat] = value;
		_currentHealth = CurrentEnemyStats[EnemyStat.MaxHealth];

		TriggerEffects(EnemyEffectTrigger.OnSpawn);
		TriggerEffects(EnemyEffectTrigger.OnTimer);

		if (Effects.TryGetValue(EnemyEffectTrigger.OnTimer, out Array<EnemyEffect> onTimerEffects) && onTimerEffects.Count > 0)
		{
			foreach (EnemyEffect effect in onTimerEffects)
			{
                Timer timer = new()
                {
                    WaitTime = effect.EffectInterval,
					Autostart = true
                };
                timer.Timeout += () => effect.ApplyEffect(this);
				TimerEffectTimers.Add(timer);
				AddChild(timer);
			}
		}

		PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize), (Vector2I)(TargetPos / PathfindingManager.instance.TileSize));
		float offsetMargin = PathfindingManager.instance.TileSize * 0.75f;
		Vector2 offset = new(_rand.RandfRange(-offsetMargin / 2f, offsetMargin / 2f), _rand.RandfRange(-offsetMargin / 2f, offsetMargin / 2f));
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
			{
				if (BuildingManager.instance.IsInsideTree())
					BuildingManager.instance.TakeDamage(Mathf.FloorToInt(CurrentEnemyStats[EnemyStat.Damage]));
				QueueFree();
			}
		}
		else
		{
			Velocity = Velocity.Lerp(Vector2.Zero, _deceleration * (float)delta);
		}

		if (IsInsideTree())
			MoveAndSlide();
	}

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
	{
		if (Mathf.RoundToInt(amount * 100) == 0)
			return amount;
		
		if (amount < 0)
			damageType = DamageType.Heal;

		if (!_isDead)
		{
			float damageDealt = amount;
			if (!defenceBreak)
				damageDealt = DamageAfterArmorPierce(damageDealt);

			_currentHealth -= damageDealt;
			_currentHealth = Mathf.Clamp(_currentHealth, 0, CurrentEnemyStats[EnemyStat.MaxHealth]);

			InstantiateDamageNumber(damageDealt, damageType);

			TriggerEffects(EnemyEffectTrigger.OnThreshold);

			if (_currentHealth <= 0)
				Die();
			
			// Debug thing
			if (_showMaxHp)
				GetChild<RichTextLabel>(2).Text = _currentHealth.ToString() + '/' + CurrentEnemyStats[EnemyStat.MaxHealth];

			return damageDealt;
		}

		return float.NaN;
	}

	public virtual void AddStatusEffectStacks(StatusEffect status, float statusStacks, bool decay = false)
	{
		// If status effect applied start decay timer of it if timer exists
		if (!decay && _currentStatusEffects[status] <= 0f && _currentStatusEffectDecayTimers.TryGetValue(status, out Timer decayTimer))
            decayTimer.Start();

		// Apply status effects stacks
		_currentStatusEffects[status] += statusStacks;
		_currentStatusEffects[status] = Mathf.Max(_currentStatusEffects[status], 0);

		// If decayed but stacks sill above 0 then restart timer until it reaches 0
		if (decay && _currentStatusEffects[status] > 0)
			_currentStatusEffectDecayTimers[status].Start();

		// If status is a ticking status and the timer isnt already started then start ticking timer
		if (_currentStatusEffectTickTimers.TryGetValue(status, out Timer tickTimer) && tickTimer.IsStopped() && _currentStatusEffects[status] > 0)
			tickTimer.Start();

		// Special effects if status effects reach certain requirements
		if (StatusEffectsData.DoesStatusEffectHaveThreshold(status))
			StatusEffectsData.DoEnemyStatusThresholdBehaviour(status, this);
		
		UpdateStats();
	}

	public void SetStatusEffectValue(StatusEffect status, float amount)
	{
		_currentStatusEffects[status] = amount;
	}

	public void TickStatusEffect(StatusEffect status)
	{
		if (_currentStatusEffects[status] > 0f && StatusEffectsData.IsStatusEfectTicking(status))
		{
			_currentStatusEffectTickTimers[status].Start();
			StatusEffectsData.TickEnemyStatusEffect(status, this);
		}
	}

	public float GetCurrentEnemyStatusEffectStacks(StatusEffect status)
	{
		if (_currentStatusEffects.TryGetValue(status, out float value))
			return value;
		else
			return 0f;
	}

	public float GetCurrentHealth()
	{
		return _currentHealth;
	}

	protected virtual void MoveToNextPathPoint(float delta, float speedMult = 1f)
	{
		Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);

		Velocity = Velocity.Lerp(dir.Normalized() * CurrentEnemyStats[EnemyStat.Speed] * speedMult, _acceleration * delta);
		Sprite.Rotation = Mathf.LerpAngle(Sprite.Rotation, dir.Angle(), _acceleration * delta);
	}

	protected virtual void Die()
	{
		_isDead = true;

		TriggerEffects(EnemyEffectTrigger.OnDeath);

		QueueFree();
	}

	protected virtual void UpdateStats()
	{
		foreach(EnemyStat stat in CurrentEnemyStats.Keys)
		{
			float value = _baseEnemyStats[stat];

			foreach ((StatusEffect status, float stacks) in _currentStatusEffects)
			{
				if (stacks > 0 && StatusEffectsData.DoesStatusMultiplyEnemyStat(status, stat))
					value *= StatusEffectsData.GetEnemyStatusStatMultiplier(status, stat, this);
			}

			float healAmount = -1f;
			if (stat == EnemyStat.MaxHealth && value > CurrentEnemyStats[stat])
			{
				healAmount = value - CurrentEnemyStats[stat];
			}

			CurrentEnemyStats[stat] = value;

			if (healAmount > 0)
				TakeDamage(-healAmount, DamageType.Heal, true);

			_currentHealth = Mathf.Clamp(_currentHealth, 0, CurrentEnemyStats[EnemyStat.MaxHealth]);
		}
	}

	protected void TriggerEffects(EnemyEffectTrigger triggerEvent)
	{
		if (Effects.TryGetValue(triggerEvent, out Array<EnemyEffect> effects) && effects.Count > 0)
		{
			foreach (EnemyEffect effect in effects)
			{
				if (triggerEvent == EnemyEffectTrigger.OnThreshold && _currentHealth / CurrentEnemyStats[EnemyStat.MaxHealth] < effect.HealthPercentageThreshold)
				{
					effect.ApplyEffect(this);
				}
				else if (triggerEvent != EnemyEffectTrigger.OnThreshold)
					effect.ApplyEffect(this);
			}
		}
	}

	protected void InstantiateDamageNumber(float damageDealt, DamageType damageType)
	{
		DamageNumber damageNumber = _damageNumberScene.Instantiate<DamageNumber>();
		damageNumber.DamageValue = Mathf.Round(damageDealt * 100) / 100;
		damageNumber.DamageTypeDealt = damageType;
		damageNumber.GlobalPosition = GlobalPosition + new Vector2(_rand.RandfRange(-5f, 5f), _rand.RandfRange(-1f, 1f));
		EnemyManager.instance.EnemyParent.AddChild(damageNumber);
	}

	protected float DamageAfterArmorPierce(float originalDamage)
	{
		if (CurrentEnemyStats.TryGetValue(EnemyStat.Defence, out float defence))
			return originalDamage * Mathf.Pow(0.975f, defence - 1);
		else
			return originalDamage;
	}

	public void RestoreOriginalColor()
	{
		Modulate = Colors.White;
	}
}