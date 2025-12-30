using Godot;
using Godot.Collections;

public static class DamageTypeData
{
	public static readonly Dictionary<DamageType, Color> _damageTypeColors = new()
	{
		{DamageType.Physical, new Color("#dadada")},
		{DamageType.Chill, new Color("#6ddcff")},
		{DamageType.Poison, new Color("#d976f2ff")},
		{DamageType.Burn, new Color("#f95e5eff")}
	};

	public static Color GetDamageTypeColor(DamageType damageType)
	{
		return _damageTypeColors[damageType];
	}

	public static Color GetMultipleDamageTypeColor(Array<DamageType> damageTypes)
	{
		Color colorToReturn = GetDamageTypeColor(damageTypes[0]);
		for (int i = 1; i < damageTypes.Count; i++)
		{
			colorToReturn = (colorToReturn + GetDamageTypeColor(damageTypes[i])).Clamp();
		}

		return colorToReturn;
	}
}