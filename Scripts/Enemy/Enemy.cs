using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyEffectTrigger, Array<EnemyEffect>> Effects = [];
	[Export] private Dictionary<EnemyStat, int> _baseEnemyStats = [];
	[Export] private float _acceleration = 5f;
	[Export] private float _deceleration = 10f;
	[Export] private float _offsetMargin = 0.4f;
	[Export] private PackedScene _damageNumberScene;

	public Vector2 TargetPos;
	public Array<Vector2> PathArray = [];
	public Dictionary<EnemyStat, float> CurrentEnemyStats = [];
	public int SpawnedWave = -1;

	private Dictionary<StatusEffect, float> _currentStatusEffects = [];
	private Dictionary<StatusEffect, Timer> _currentStatusEffectDecayTimers = [];
	private Dictionary<StatusEffect, Timer> _currentStatusEffectTickTimers = [];
	private Array<Timer> _timerEffectTimers = [];
	private Sprite2D _sprite;
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

		_sprite = GetChild<Sprite2D>(0);

		foreach ((EnemyStat stat, int value) in _baseEnemyStats)
			CurrentEnemyStats[stat] = value;
		_currentHealth = CurrentEnemyStats[EnemyStat.MaxHealth];

		TriggerEffects(EnemyEffectTrigger.OnSpawn);

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
				_timerEffectTimers.Add(timer);
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

		if (!_isDead)
		{
			float damageDealt = amount;
			if (!defenceBreak)
				damageDealt = DamageAfterArmorPierce(damageDealt);

			_currentHealth -= damageDealt;

			InstantiateDamageNumber(damageDealt, damageType);

			TriggerEffects(EnemyEffectTrigger.OnDamage);

			if (_currentHealth <= 0)
				Die();
			
			// Debug thing
			if (_showMaxHp)
				GetChild<RichTextLabel>(2).Text = _currentHealth.ToString() + '/' + CurrentEnemyStats[EnemyStat.MaxHealth];

			return damageDealt;
		}

		return float.NaN;
	}

	public void AddStatusEffectStacks(StatusEffect status, float statusStacks, bool decay = false)
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
		switch (status)
		{
			case StatusEffect.Chill: // If chill reaches max value convert it all to stun and recolor enemy slightly for a brief period
				// Cant chill enemies that are frozen
				if (Modulate == new Color(0.17f, 1f, 1f, 1f))
					_currentStatusEffects[status] -= statusStacks;

				if (_currentStatusEffects[status] >= StatusEffectsData.GetMaxStatusEffectValue(status))
				{
					AddStatusEffectStacks(StatusEffect.Stun, _currentStatusEffects[status]);
					
					Modulate = new Color(0.17f, 1f, 1f, 1f);
					Timer timer = new()
					{
						WaitTime = _currentStatusEffects[status] / 10f,
						Autostart = true,
						OneShot = true
					};
					timer.Connect(Timer.SignalName.Timeout, Callable.From(RestoreOriginalColor));
					timer.Connect(Timer.SignalName.Timeout, Callable.From(timer.QueueFree));
					AddChild(timer);

					_currentStatusEffects[status] = 0f;
				}
				break;
			case StatusEffect.Stun: // Effects that run on the timer pause while stunned, like summoning enemies
				if (_currentStatusEffects[status] > 0)
				{
					foreach (Timer timer in _timerEffectTimers)
					{
						if (!timer.Paused)
							timer.Paused = true;
					}
				}
				else
				{
					foreach (Timer timer in _timerEffectTimers)
					{
						if (timer.Paused)
							timer.Paused = false;
					}
				}
				break;
		}
	}

	public void TickStatusEffect(StatusEffect status)
	{
		if (_currentStatusEffects[status] > 0f && StatusEffectsData.IsStatusEfectTicking(status))
		{
			_currentStatusEffectTickTimers[status].Start();
			switch (status)
			{
				case StatusEffect.Poison:
					TakeDamage(CurrentEnemyStats[EnemyStat.MaxHealth] * Mathf.Min(0.001f * _currentStatusEffects[status], 0.07f), DamageType.Poison, true);
					// Do something with excess poison stacks
					break;
				case StatusEffect.Burn:
					int burnExplosionThreshold = (int)(StatusEffectsData.GetMaxStatusEffectValue(StatusEffect.Burn) * Mathf.FloorToInt(_currentStatusEffects[status] / StatusEffectsData.GetMaxStatusEffectValue(StatusEffect.Burn)));

					float burnToConsume = _currentStatusEffects[status] * 0.75f;
                    TakeDamage(burnToConsume * 0.05f, DamageType.Burn, false);
					_currentStatusEffects[status] -= burnToConsume;

					if (_currentStatusEffects[status] < burnExplosionThreshold)
					{
						burnToConsume = _currentStatusEffects[status] * 0.25f;
						TakeDamage(burnToConsume * 0.1f, DamageType.Burn, false);
						_currentStatusEffects[status] -= burnToConsume;
					}
					break;
			}
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

		if (_currentStatusEffects[StatusEffect.Chill] > 0f)
			speed *= 6.4f / Mathf.Pow(_currentStatusEffects[StatusEffect.Chill] + 3f, 2f) + 0.35f;

		if (_currentStatusEffects[StatusEffect.Poison] > 0f)
			speed *= 0.95f;

		if (_currentStatusEffects[StatusEffect.Stun] > 0)
			speed = 0;

		if (_currentStatusEffects[StatusEffect.Burn] > 0f)
			speed *= 1.1f;

		CurrentEnemyStats[EnemyStat.Speed] = speed;
		return speed;
	}

	protected void TriggerEffects(EnemyEffectTrigger triggerEvent)
	{
		if (Effects.TryGetValue(triggerEvent, out Array<EnemyEffect> effects) && effects.Count > 0)
		{
			foreach (EnemyEffect effect in effects)
				effect.ApplyEffect(this);
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

	private void RestoreOriginalColor()
	{
		Modulate = Colors.White;
	}
}