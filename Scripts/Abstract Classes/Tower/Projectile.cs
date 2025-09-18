using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Projectile : TowerComponent
{
    [Export] protected PackedScene ProjectileScene;
    [Export] public Array<TowerEffect> Effects = [];

    // Fire the projectile, SHOULD instantiate ProjectileScene
    public abstract Node2D InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint);
}
