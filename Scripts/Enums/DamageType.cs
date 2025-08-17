using Godot;
using Godot.Collections;

public enum DamageType
{
	Physical
}

public static class DamageTypeColor
{
	public static readonly Dictionary<DamageType, Color> _damageTypeColors = new()
	{
		{DamageType.Physical, new Color("#dadada")}
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
			colorToReturn.Blend(GetDamageTypeColor(damageTypes[i]));
		}

		return colorToReturn;
	}
}