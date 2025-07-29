using Godot;
using Godot.Collections;
using System;

public abstract partial class TowerComponent : Resource
{
	[Export] public Dictionary<TowerStat, float> StatMultipliers;
	[Export] public int PointCost;
}
