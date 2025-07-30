using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyStat, int> BaseEnemyStats = [];
	[Export] public Dictionary<EnemyEffectTrigger, EnemyEffect> Effects = [];
	[Export] public float acceleration = 5f;

	public Marker2D targetPos;

	private float _currentHealth;
	private Dictionary<EnemyStat, int> _currentEnemyStats;
	private Array<Vector2I> _pathArray = [];

	public override void _Ready()
	{
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

		_pathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / 64), (Vector2I)(targetPos.Position / 64));
	}

	public override void _Process(double delta)
	{
		if (_pathArray.Count > 0)
		{
			Vector2 dir = GlobalPosition.DirectionTo(_pathArray[0]);

			Velocity = Velocity.Lerp(dir.Normalized() * _currentEnemyStats[EnemyStat.Speed], acceleration * (float)delta);

			if (GlobalPosition.DistanceTo(_pathArray[0]) <= 10)
			{
				_pathArray.RemoveAt(0);
			}
		}
		else
		{
			Velocity = Vector2.Zero;
		}

		MoveAndSlide();
	}

	// Returns Damage Dealt
	public virtual float TakeDamage(float amount, bool defenceBreak = false)
	{
		float damageDealt = amount;
		if (!defenceBreak && BaseEnemyStats.TryGetValue(EnemyStat.Defence, out int value))
			damageDealt *= Mathf.Pow(0.975f, value);

		_currentHealth -= damageDealt;

		TriggerEffects(EnemyEffectTrigger.OnDamage);

		if (_currentHealth <= 0)
		{
			Die();
		}

		return damageDealt;
	}

	protected virtual void Die()
	{
		TriggerEffects(EnemyEffectTrigger.OnDeath);

		QueueFree();
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
}

public enum EnemyStat
{
	MaxHealth,
	Speed,
	Damage,
	Defence
}

public enum EnemyEffectTrigger
{
    OnSpawn,
    OnDeath,
    OnDamage,
    OnTimer
}