using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Projectile : Resource
{
    [Export] public Dictionary<Stat, float> StatMultipliers;
    [Export] protected PackedScene ProjectileScene;
    [Export] public int PointCost;
    public Array<Effect> Effects;

    // Fire the projectile, SHOULD instantiate ProjectileScene
    public abstract void InstantiateProjectile(Dictionary<Stat, float> finalStats, Marker2D firePoint);
}
