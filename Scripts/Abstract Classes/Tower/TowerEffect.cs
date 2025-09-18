using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class TowerEffect : TowerComponent
{
    [Export] public DamageType DamageType = DamageType.Physical;
    
    // Example of an effect could be damage effect, so dealing damage SHOULD also be implemented as an effect
    public abstract void ApplyEffect(Dictionary<TowerStat, float> towerStats, Enemy target); // Will take in "target" variable for enemy
}
