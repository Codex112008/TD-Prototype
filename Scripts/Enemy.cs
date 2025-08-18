using System;
using System.Linq;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyStat, int> BaseEnemyStats = [];
	[Export] public Dictionary<EnemyEffectTrigger, EnemyEffect> Effects = [];
	[Export] public float acceleration = 5f;
	[Export] public float deceleration = 10f;

	[Export] private PackedScene _damageNumberScene;

	public Vector2 targetPos;
	public Array<Vector2> PathArray = [];
	private Dictionary<StatusEffect, int> _currentStatusEffects = [];

	private Vector2 _offset;
	private Sprite2D _sprite;
	private float _currentHealth;
	private Dictionary<EnemyStat, int> _currentEnemyStats;

	public override void _Ready()
	{
		// Initialises status efects dictionary
		foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)).Cast<StatusEffect>())
			_currentStatusEffects.Add(status, 0);

		_sprite = GetChild<Sprite2D>(0);

		_currentHealth = BaseEnemyStats[EnemyStat.MaxHealth];
		_currentEnemyStats = BaseEnemyStats;
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
					timer.Timeout += effect.ApplyEffect;

					timer.Start();
				}
			}
		}

		PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize), (Vector2I)(targetPos / PathfindingManager.instance.TileSize));
		RandomNumberGenerator rand = new();
		float offsetMargin = PathfindingManager.instance.TileSize * 0.75f;
		_offset = new(rand.RandfRange(-offsetMargin / 2, offsetMargin / 2), rand.RandfRange(-offsetMargin / 2, offsetMargin / 2));
		rand.Dispose();
	}

	public override void _Process(double delta)
	{
		if (PathArray.Count > 0)
		{
			Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);

			Velocity = Velocity.Lerp(dir.Normalized() * CalculateSpeed(), acceleration * (float)delta);
			_sprite.Rotation = Mathf.LerpAngle(_sprite.Rotation, dir.Angle(), acceleration * (float)delta);

			if (GlobalPosition.DistanceTo(PathArray[0]) <= 10)
			{
				PathArray.RemoveAt(0);
			}
		}
		else
		{
			Velocity = Velocity.Lerp(Vector2.Zero, deceleration * (float)delta);
		}

		MoveAndSlide();
	}

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
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

	public void ModifyStatusEffectStacks(StatusEffect status, int amount)
	{
		_currentStatusEffects[status] += amount;
	}

	protected virtual void Die()
	{
		TriggerEffects(EnemyEffectTrigger.OnDeath);

		QueueFree();
	}

	protected virtual float CalculateSpeed()
	{
		float realSpeed = _currentEnemyStats[EnemyStat.Speed];

		if (_currentStatusEffects[StatusEffect.Chill] > 0)
			realSpeed *= 6.4f / Mathf.Pow(_currentStatusEffects[StatusEffect.Chill] + 3, 2) + 0.35f;

		return realSpeed;
	}

	protected void TriggerEffects(EnemyEffectTrigger triggerEvent)
	{
		if (Effects != null && Effects.Count != 0)
		{
			foreach ((EnemyEffectTrigger trigger, EnemyEffect effect) in Effects)
			{
				if (trigger == triggerEvent)
				{
					effect.ApplyEffect();
				}
			}
		}
	}

	protected void InstantiateDamageNumber(float damageDealt, DamageType damageType)
	{
		DamageNumber damageNumber = _damageNumberScene.Instantiate<DamageNumber>();
		damageNumber.DamageValue = Mathf.Round(damageDealt * 100) / 100;
		damageNumber.DamageTypeDealt = damageType;
		damageNumber.GlobalPosition = GlobalPosition;
		EnemyManager.instance.EnemyParent.AddChild(damageNumber);
	}

	protected float DamageAfterArmorPierce(float originalDamage)
	{
		if (BaseEnemyStats.TryGetValue(EnemyStat.Defence, out int defence))
			return originalDamage * Mathf.Pow(0.975f, defence);
		else
			return originalDamage;
	}
}