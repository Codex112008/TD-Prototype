using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class TowerEffect : TowerComponent
{
    // Apply the unique effect, SHOULD be called in projectile's Fire() method
    // Examle of an effect could be damage effect, so dealing damage SHOULD also be implemented as an effect
    public abstract void ApplyEffect(Dictionary<TowerStat, float> stats, Enemy target); // Will take in "target" variable for enemy
}
