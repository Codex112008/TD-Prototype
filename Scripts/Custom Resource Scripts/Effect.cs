using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Effect : Resource
{
    [Export] public Dictionary<Stat, float> StatMultipliers;
    [Export] public int PointCost;

    // Apply the unique effect, SHOULD be called in projectile's Fire() method
    // Examle of an effect could be damage effect, so dealing damage SHOULD also be implemented as an effect
    public abstract void ApplyEffect(Dictionary<Stat, float> stats, Enemy target); // Will take in "target" variable for enemy
}
