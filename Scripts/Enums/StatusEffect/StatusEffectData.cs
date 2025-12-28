using Godot;
using Godot.Collections;
using System;

public static class StatusEffectsData
{
	private static readonly Dictionary<StatusEffect, float> _statusEffectDurations = new()
	{
		{StatusEffect.Chill, 0.6f},
		{StatusEffect.Poison, -1f},
		{StatusEffect.Stun, 0.1f}
	};

	private static readonly Dictionary<StatusEffect, float> _maxStatusEffectValues = new()
	{
		{StatusEffect.Chill, 10f},
	};

	private static readonly Dictionary<StatusEffect, float> _tickingStatusEffects = new()
	{ // Values of this dictionary are the base tick intervals of the status effect
		{StatusEffect.Poison, 1f}
	};

	public static float GetStatusEffectDuration(StatusEffect status)
	{
		return _statusEffectDurations[status];
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
}