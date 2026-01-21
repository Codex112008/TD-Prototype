using Godot;
using Godot.Collections;
using System;

public static class StatusEffectsData
{
	private static readonly Dictionary<StatusEffect, float> _statusEffectDecayCooldowns = new()
	{
		{StatusEffect.Chill, 0.6f},
		{StatusEffect.Stun, 0.1f},
		{StatusEffect.Poison, 10f},
		{StatusEffect.Regen, 3f},
		{StatusEffect.Reinforcement, 1f},
		{StatusEffect.Swiftness, 1f},
		{StatusEffect.Surge, 1f},
	};

	private static readonly Dictionary<StatusEffect, float> _statusEffectThresholds = new()
	{
		{StatusEffect.Chill, 20f},
		{StatusEffect.Stun, 20f},
	};

	private static readonly Dictionary<StatusEffect, float> _tickingStatusEffects = new()
	{ // Values of this dictionary are the base tick intervals of the status effect
		{StatusEffect.Poison, 1f},
		{StatusEffect.Burn, 0.75f},
		{StatusEffect.Regen, 1f},
		{StatusEffect.Bleed, 0.8f},
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
				{StatusEffect.Poison, Callable.From((Enemy enemy) => Mathf.Max(0.01f, 1f - (Mathf.Max(0f, enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Poison) - 40f) * 0.01f)))},
			}
		},
		{EnemyStat.MaxHealth,
			new()
			{
				{StatusEffect.Surge, Callable.From((Enemy enemy) => (float)Math.Log2((enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Reinforcement) + 15f) / 15f) + 1)},
				{StatusEffect.Poison, Callable.From((Enemy enemy) => Mathf.Max(0f, 1f - (Mathf.Max(0f, enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Poison) - 140f) * 0.02f)))},
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

				if (enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill) >= GetStatusEffectThreshold(StatusEffect.Chill))
				{
					enemy.AddStatusEffectStacks(StatusEffect.Stun, enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Chill) / 4f);
					
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
				float stunAmount = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Stun);
				if (stunAmount > 0)
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
				enemy.TakeDamage(enemy.CurrentEnemyStats[EnemyStat.MaxHealth] * Mathf.Min(0.0025f * enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Poison), 0.1f), DamageType.Poison, true);
			})
		},
		{StatusEffect.Burn, Callable.From((Enemy enemy) =>
			{
				float burnToConsume = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn) * 0.75f;
				enemy.TakeDamage(burnToConsume * 0.07f, DamageType.Burn, true);
				enemy.AddStatusEffectStacks(StatusEffect.Burn, -burnToConsume);

				if (burnToConsume > 50)
				{
					burnToConsume = enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Burn);
					enemy.TakeDamage(burnToConsume * 0.15f, DamageType.Burn, true);
					enemy.AddStatusEffectStacks(StatusEffect.Burn, -burnToConsume);;
				}
			})
		},
		{StatusEffect.Regen, Callable.From((Enemy enemy) =>
			{
				enemy.TakeDamage(-enemy.CurrentEnemyStats[EnemyStat.MaxHealth] * Mathf.Min(0.01f * enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Regen), 1f), DamageType.Heal, true);
				// Do something with excess regen stacks
			})
		},
		{StatusEffect.Bleed, Callable.From((Enemy enemy) => 
			{ // Does more damage based on the speed of enemy
				enemy.TakeDamage(((2.8f * (float)Math.Log10(enemy.CurrentEnemyStats[EnemyStat.Speed] + 12.1f)) - 3.031799f) * (enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Bleed) * 0.5f), DamageType.Physical, false);
				enemy.AddStatusEffectStacks(StatusEffect.Bleed, (100f / enemy.CurrentEnemyStats[EnemyStat.Speed]) + 1);
			})
		},
	};

	public static float GetStatusDecayCooldown(StatusEffect status)
	{
		return _statusEffectDecayCooldowns[status];
	}

	public static float GetStatusEffectThreshold(StatusEffect status)
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