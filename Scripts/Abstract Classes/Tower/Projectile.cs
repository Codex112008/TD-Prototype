using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Projectile : TowerComponent
{
    [Export] protected PackedScene ProjectileScene;
    [Export] public Array<TowerEffect> Effects = [];

    // Fire the projectile, SHOULD instantiate ProjectileScene
    public abstract void InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint);
}
