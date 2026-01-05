using Godot;
using Godot.Collections;
using System;

public static class StatusEffectsData
{
	private static readonly Dictionary<StatusEffect, float> _statusEffectDecayCooldowns = new()
	{
		{StatusEffect.Chill, 0.6f},
		{StatusEffect.Stun, 0.025f},
		{StatusEffect.Poison, 1.5f},
		{StatusEffect.Regen, 3f},
		{StatusEffect.Reinforcement, 1f},
		{StatusEffect.Swiftness, 1f},
		{StatusEffect.Surge, 1f},
	};

	private static readonly Dictionary<StatusEffect, float> _statusEffectThresholds = new()
	{
		{StatusEffect.Chill, 10f},
		{StatusEffect.Stun, 50f},
	};

	private static readonly Dictionary<StatusEffect, float> _tickingStatusEffects = new()
	{ // Values of this dictionary are the base tick intervals of the status effect
		{StatusEffect.Poison, 1f},
		{StatusEffect.Burn, 0.75f},
		{StatusEffect.Regen, 1f},
	};

	private static readonly Dictionary<EnemyStat, Dictionary<StatusEffect, Callable>> _enemyStatusStatMultipliers = new()
	{
		{EnemyStat.Speed,
			new()
			{
				{StatusEffect.Chill, Callable.From((Enemy enemy) => 6.4f / Mathf.Pow(enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill) + 3f, 2f) + 0.35f)},
				{StatusEffect.Poison, Callable.From((Enemy enemy) => 0.95f)},
				{StatusEffect.Stun, Callable.From((Enemy enemy) => 0f)},
				{StatusEffect.Burn, Callable.From((Enemy enemy) => 1.1f)},
				{StatusEffect.Swiftness, Callable.From((Enemy enemy) => (float)Math.Log10(enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Swiftness) + 10f))},
			}
		},
		{EnemyStat.Defence,
			new()
			{
				{StatusEffect.Reinforcement, Callable.From((Enemy enemy) => (float)Math.Log2((enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Reinforcement) + 15f) / 15f) + 1)},
			}
		},
		{EnemyStat.MaxHealth,
			new()
			{
				{StatusEffect.Surge, Callable.From((Enemy enemy) => (float)Math.Log2((enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Reinforcement) + 15f) / 15f) + 1)},
			}
		},
	};

	private static readonly Dictionary<StatusEffect, Callable> _enemyStatusEffectThresholdBehaviours = new()
	{
		{StatusEffect.Chill, Callable.From((Enemy enemy) => // If chill reaches max value convert it all to stun and recolor enemy slightly for a brief period
			{	
				// Cant chill enemies that are frozen
				if (enemy.Modulate == new Color(0.17f, 1f, 1f, 1f))
					enemy.SetStatusEffectValue(StatusEffect.Chill, 0);

				if (enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill) >= GeStatusEffectThreshold(StatusEffect.Chill))
				{
					enemy.AddStatusEffectStacks(StatusEffect.Stun, enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill));
					
					enemy.Modulate = new Color(0.17f, 1f, 1f, 1f);
					Timer timer = new()
					{
						WaitTime = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill) / 40f,
						Autostart = true,
						OneShot = true
					};
					timer.Connect(Timer.SignalName.Timeout, Callable.From(enemy.RestoreOriginalColor));
					timer.Connect(Timer.SignalName.Timeout, Callable.From(timer.QueueFree));
					enemy.AddChild(timer);

					enemy.SetStatusEffectValue(StatusEffect.Chill, 0);
				}
			})
		},
		{StatusEffect.Stun, Callable.From((Enemy enemy) => // Effects that run on the timer pause while stunned, like summoning enemies
			{	
				if (enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Stun) > 0)
				{
					foreach (Timer timer in enemy.TimerEffectTimers)
					{
						if (!timer.Paused)
							timer.Paused = true;
					}
				}
				else
				{
					foreach (Timer timer in enemy.TimerEffectTimers)
					{
						if (timer.Paused)
							timer.Paused = false;
					}
				}
			})
		},
	};

	private static readonly Dictionary<StatusEffect, Callable> _tickEnemyStatusEffectBehaviours = new()
	{
		{StatusEffect.Poison, Callable.From((Enemy enemy) => 
			{
				enemy.TakeDamage(enemy.CurrentEnemyStats[EnemyStat.MaxHealth] * Mathf.Min(0.001f * enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Poison), 0.07f), DamageType.Poison, true);
				// Do something with excess poison stacks
			})
		},
		{StatusEffect.Burn, Callable.From((Enemy enemy) =>
			{
				int burnExplosionThreshold = (int)(GeStatusEffectThreshold(StatusEffect.Burn) * Mathf.FloorToInt(enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn) / GeStatusEffectThreshold(StatusEffect.Burn)));

				float burnToConsume = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn) * 0.75f;
                enemy.TakeDamage(burnToConsume * 0.05f, DamageType.Burn, false);
				enemy.AddStatusEffectStacks(StatusEffect.Burn, -burnToConsume);

				if (enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn) < burnExplosionThreshold)
				{
					burnToConsume = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn) * 0.25f;
					enemy.TakeDamage(burnToConsume * 0.1f, DamageType.Burn, false);
					enemy.AddStatusEffectStacks(StatusEffect.Burn, -burnToConsume);;
				}
			})
		},
		{StatusEffect.Regen, Callable.From((Enemy enemy) => 
			{
				enemy.TakeDamage(enemy.CurrentEnemyStats[EnemyStat.MaxHealth] * Mathf.Min(0.01f * enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Regen), 1f), DamageType.Heal, true);
				// Do something with excess regen stacks
			})
		},
	};

	public static float GetStatusDecayCooldown(StatusEffect status)
	{
		return _statusEffectDecayCooldowns[status];
	}

	public static float GeStatusEffectThreshold(StatusEffect status)
	{
		return _statusEffectThresholds[status];
	}
	
	public static float GetBaseStatusEffectTickInterval(StatusEffect status)
	{
		return _tickingStatusEffects[status];
	}

	public static bool IsStatusEfectTicking(StatusEffect status)
	{
		return _tickingStatusEffects.ContainsKey(status);
	}

	public static bool DoesStatusEffectHaveThreshold(StatusEffect status)
	{
		return _statusEffectThresholds.ContainsKey(status);
	}

	public static bool DoesStatusEfectDecay(StatusEffect status)
	{
		return _statusEffectDecayCooldowns.ContainsKey(status);
	}

	public static bool DoesStatusMultiplyEnemyStat(StatusEffect status, EnemyStat stat)
	{
		if (_enemyStatusStatMultipliers.TryGetValue(stat, out Dictionary<StatusEffect, Callable> value))
			return value.ContainsKey(status);
		return false;
	}

	public static float GetEnemyStatusStatMultiplier(StatusEffect status, EnemyStat stat, Enemy enemy)
	{
		return (float)_enemyStatusStatMultipliers[stat][status].Call(enemy);
    }

	public static void TickEnemyStatusEffect(StatusEffect status, Enemy enemy)
	{
		_tickEnemyStatusEffectBehaviours[status].Call(enemy);
	}

	public static void DoEnemyStatusThresholdBehaviour(StatusEffect status, Enemy enemy)
	{
		_enemyStatusEffectThresholdBehaviours[status].Call(enemy);
	}
}