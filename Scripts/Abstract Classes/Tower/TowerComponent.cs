using Godot;
using Godot.Collections;
using System;

public abstract partial class TowerComponent : Resource
{
	[Export(PropertyHint.MultilineText)] public string Tooltip;
	[Export] public Texture2D Icon;
	[Export] public Dictionary<TowerStat, float> StatMultipliers;
	[Export] public int PointCost;
}
