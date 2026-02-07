using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : PathfindingEntity
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
	[Export] private PackedScene _damageNumberScene;

	public Dictionary<EnemyStat, float> CurrentEnemyStats = [];
	public Array<Timer> TimerEffectTimers = [];
	public int SpawnedWave = -1;
	public bool RegisterDeathSignal = true;

	protected Dictionary<StatusEffect, float> _currentStatusEffects = [];
	protected Dictionary<StatusEffect, Timer> _currentStatusEffectDecayTimers = [];
	protected bool _isDead = true;
	
	private Dictionary<StatusEffect, Timer> _currentStatusEffectTickTimers = [];
	private float _currentHealth;
	private bool _showMaxHp = true;

	public override void _Ready()
	{
		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(2).Visible = true;
			GetChild<RichTextLabel>(2).Text = _baseEnemyStats[EnemyStat.MaxHealth].ToString() + '/' + _baseEnemyStats[EnemyStat.MaxHealth];
		}

		Sprite ??= GetChild<Sprite2D>(0);

		// Initialises timers for effects
		if (Effects.TryGetValue(EnemyEffectTrigger.OnTimer, out Array<EnemyEffect> onTimerEffects) && onTimerEffects.Count > 0)
		{
			foreach (EnemyEffect effect in onTimerEffects)
			{
                Timer timer = new()
                {
                    WaitTime = effect.EffectInterval,
                };
                timer.Timeout += () => effect.ApplyEffect(this);
				TimerEffectTimers.Add(timer);
				AddChild(timer);
			}
		}

		// Initialises status efects dictionary
		_currentStatusEffects = [];
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

		Init();

		TreeEntered += Init;
	}

	private void Init()
	{
		// Reset enemy stats to base enemy stats
		CurrentEnemyStats = [];
		foreach ((EnemyStat stat, int value) in _baseEnemyStats)
			CurrentEnemyStats.Add(stat, value);
		_currentHealth = CurrentEnemyStats[EnemyStat.MaxHealth];

		if (_showMaxHp)
		{
			GetChild<RichTextLabel>(2).Visible = true;
			GetChild<RichTextLabel>(2).Text = CurrentEnemyStats[EnemyStat.MaxHealth].ToString() + '/' + _baseEnemyStats[EnemyStat.MaxHealth];
		}

		// Trigger effects that happen when spawning
		TriggerEffects(EnemyEffectTrigger.OnSpawn);
		TriggerEffects(EnemyEffectTrigger.OnTimer);

		// Start timers for timer effects
		foreach (Timer timer in TimerEffectTimers)
		{
			if (IsInsideTree())
				timer.Start();
			else
				timer.Autostart = true;
		}
		
		base._Ready();

		// If grabbing from pool need to reset this so the enemy can take damage and die
		_isDead = false;
	}

    public override void _PhysicsProcess(double delta)
    {
		_speed = CurrentEnemyStats[EnemyStat.Speed];

        base._PhysicsProcess(delta);
    }

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
	{
		if (Mathf.RoundToInt(amount * 100) == 0)
			return amount;
		
		if (amount < 0)
			damageType = DamageType.Heal;

		if (!_isDead && IsInsideTree())
		{
			float damageDealt = amount;
			if (!defenceBreak)
				damageDealt = DamageAfterArmorPierce(damageDealt);

			_currentHealth -= damageDealt;
			_currentHealth = Mathf.Clamp(_currentHealth, 0, CurrentEnemyStats[EnemyStat.MaxHealth]);
			_currentHealth = (float)Math.Round(_currentHealth, 2);

			InstantiateDamageNumber(damageDealt, damageType);

			TriggerEffects(EnemyEffectTrigger.OnThreshold);
			
			// Debug thing
			if (_showMaxHp)
				GetChild<RichTextLabel>(2).Text = _currentHealth.ToString() + '/' + CurrentEnemyStats[EnemyStat.MaxHealth];

			if (_currentHealth <= 0)
				Die();

			return damageDealt;
		}

		return float.NaN;
	}

	public virtual void AddStatusEffectStacks(StatusEffect status, float statusStacks, bool decay = false)
	{
		// If status effect applied start decay timer of it if timer exists
		if (!decay && statusStacks > 0f && _currentStatusEffectDecayTimers.TryGetValue(status, out Timer decayTimer) && IsInsideTree())
		{
			if (IsInsideTree())
				decayTimer.Start();
			else
				decayTimer.Autostart = true;
		}

		// Apply status effects stacks
		_currentStatusEffects[status] = Mathf.Max(_currentStatusEffects[status] + statusStacks, 0);

		// If decayed but stacks sill above 0 then restart timer until it reaches 0
		if (decay && _currentStatusEffects[status] > 0)
		{
			if (IsInsideTree())
				_currentStatusEffectDecayTimers[status].Start();
			else
				_currentStatusEffectDecayTimers[status].Autostart = true;
		}

		// If status is a ticking status and the timer isnt already started then start ticking timer
		if (_currentStatusEffectTickTimers.TryGetValue(status, out Timer tickTimer) && tickTimer.IsStopped() && _currentStatusEffects[status] > 0 && IsInsideTree())
		{
			if (IsInsideTree())
				tickTimer.Start();
			else
				tickTimer.Autostart = true;
		}

		// Special effects if status effects reach certain requirements
		if (StatusEffectsData.DoesStatusEffectHaveThreshold(status))
			StatusEffectsData.DoEnemyStatusThresholdBehaviour(status, this);
		
		UpdateStats();
	}

	public void SetStatusEffectValue(StatusEffect status, float amount)
	{
		if (_currentStatusEffects.ContainsKey(status))
			_currentStatusEffects[status] = amount;
		else
			_currentStatusEffects.Add(status, amount);
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

	protected virtual void Die()
	{
		_isDead = true;

		TriggerEffects(EnemyEffectTrigger.OnDeath);
		if (RegisterDeathSignal)
			EnemyManager.instance.EmitSignal(EnemyManager.SignalName.EnemyDied, this);

		if (PoolManager.instance != null)
			PoolManager.instance.AddEnemyToPool(this);
		else
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
			if (stat == EnemyStat.MaxHealth)
			{
				if (value > CurrentEnemyStats[stat])
					healAmount = value - CurrentEnemyStats[stat];
				
				if (value <= 0f)
					Die();
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
        DamageNumber damageNumber;
        if (PoolManager.instance != null)
			damageNumber = PoolManager.instance.PopDamageNumberFromPoolOrInstantiate();
		else
			damageNumber = _damageNumberScene.Instantiate<DamageNumber>();
		
		damageNumber.DamageValue = Mathf.Round(damageDealt * 100) / 100;
		damageNumber.DamageTypeDealt = damageType;
		damageNumber.GlobalPosition = GlobalPosition + new Vector2(_rand.RandfRange(-5f, 5f), _rand.RandfRange(-1f, 1f));
		AddSibling(damageNumber);
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

    protected override void ReachedPathEnd()
    {
        if (BuildingManager.instance != null && BuildingManager.instance.IsInsideTree())
			BuildingManager.instance.TakeDamage(Mathf.FloorToInt(CurrentEnemyStats[EnemyStat.Damage]));

		if (PoolManager.instance != null)
			PoolManager.instance.AddEnemyToPool(this);
		else
			QueueFree();
    }
}