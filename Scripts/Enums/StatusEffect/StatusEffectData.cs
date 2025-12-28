using Godot;
using Godot.Collections;
using System;

public static class StatusEffectsData
{
	private static readonly Dictionary<StatusEffect, float> _statusEffectDecayCooldowns = new()
	{
		{StatusEffect.Chill, 0.6f},
		{StatusEffect.Stun, 0.1f},
	};

	private static readonly Dictionary<StatusEffect, float> _maxStatusEffectValues = new()
	{
		{StatusEffect.Chill, 10f},
		{StatusEffect.Burn, 50f}
	};

	private static readonly Dictionary<StatusEffect, float> _tickingStatusEffects = new()
	{ // Values of this dictionary are the base tick intervals of the status effect
		{StatusEffect.Poison, 1f},
		{StatusEffect.Burn, 0.75f}
	};

	public static float GetStatusDecayCooldown(StatusEffect status)
	{
		return _statusEffectDecayCooldowns[status];
	}

	public static float GetMaxStatusEffectValue(StatusEffect status)
	{
		return _maxStatusEffectValues[status];
	}
	
	public static float GetBaseStatusEffectTickInterval(StatusEffect status)
	{
		return _tickingStatusEffects[status];
	}

	public static bool IsStatusEfectTicking(StatusEffect status)
	{
		return _tickingStatusEffects.ContainsKey(status);
	}

	public static bool DoesStatusEfectDecay(StatusEffect status)
	{
		return _statusEffectDecayCooldowns.ContainsKey(status);
	}
}