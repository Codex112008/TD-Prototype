using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Projectile : TowerComponent
{
    [Export] protected PackedScene ProjectileScene;
    [Export] public Array<TowerEffect> Effects = [];
    [Export] public Array<Projectile> NextTierProjectiles = [];
    [Export] public bool RequireEnemy = true;

    // Fire the projectile, SHOULD instantiate ProjectileScene
    public abstract Node2D InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos);
}
