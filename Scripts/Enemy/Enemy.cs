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
	public CollisionShape2D Collider = null;

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

		CallDeferred(MethodName.Init);

		Connect(Node.SignalName.TreeEntered, Callable.From(() => CallDeferred(MethodName.Init)));
	}

	public void Init()
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

		// Start timers for timer effects
		foreach (Timer timer in TimerEffectTimers)
		{
			GetTree().CreateTimer(timer.WaitTime / 3f).Connect(Timer.SignalName.Timeout, Callable.From(() =>
			{
				timer.EmitSignal(Timer.SignalName.Timeout);
				timer.Start();
			}));
		}
		
		base._Ready();

		// If grabbing from pool need to reset this so the enemy can take damage and die
		_isDead = false;
		
		Collider ??= (CollisionShape2D)GetChildren().First(child => child is CollisionShape2D);
		ProcessMode = ProcessModeEnum.Inherit;
	}

    public override void _PhysicsProcess(double delta)
    {
		if (Collider.Disabled && Visible)
			Collider.Disabled = false;

		if (!Visible)
			Visible = true;

		_speed = CurrentEnemyStats[EnemyStat.Speed];

        base._PhysicsProcess(delta);
    }

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
	{
		if (float.IsInfinity(amount) || float.IsNaN(amount) || !IsInsideTree() || _isDead)
            return 0f;

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
			_currentHealth = (float)Math.Round(_currentHealth, 2);

			InstantiateDamageNumber(damageDealt, damageType);

			TriggerEffects(EnemyEffectTrigger.OnDamage);
			
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
				effect.ApplyEffect(this);
			}
		}
	}

	protected void InstantiateDamageNumber(float damageDealt, DamageType damageType)
	{
        DamageNumber damageNumber;
        if (IsInstanceValid(PoolManager.instance))
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

	public float DamageBeforeArmorPierce(float damageAfterAP)
	{
		if (CurrentEnemyStats.TryGetValue(EnemyStat.Defence, out float defence))
			return damageAfterAP / Mathf.Pow(0.975f, defence - 1);
		else
			return damageAfterAP;
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