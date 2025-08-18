using Godot;
using System;
using System.Collections.Generic;

public static class StatusEffectsData
{
	private static Dictionary<StatusEffect, float> _statusEffectDurations = new()
	{
		{StatusEffect.Chill, 0.6f}
	};

	public static float GetStatusEffectDuration(StatusEffect status)
	{
		return _statusEffectDurations[status];
	}
}