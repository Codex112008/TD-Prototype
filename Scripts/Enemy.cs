using Godot;
using Godot.Collections;

[GlobalClass]
public partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyStat, int> BaseEnemyStats = [];
	[Export] public Dictionary<EnemyEffectTrigger, EnemyEffect> Effects = [];
	[Export] public float acceleration = 5f;
	[Export] public float deceleration = 10f;

	public Vector2 targetPos;
	public Array<Vector2> PathArray = [];

	private Sprite2D _sprite;
	private float _currentHealth;
	private Dictionary<EnemyStat, int> _currentEnemyStats;

	public override void _Ready()
	{
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

		PathArray = PathfindingManager.instance.GetValidPath((Vector2I)(GlobalPosition / 64), (Vector2I)(targetPos / 64));
	}

	public override void _Process(double delta)
	{
		if (PathArray.Count > 0)
		{
			Vector2 dir = GlobalPosition.DirectionTo(PathArray[0]);

			Velocity = Velocity.Lerp(dir.Normalized() * _currentEnemyStats[EnemyStat.Speed], acceleration * (float)delta);
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