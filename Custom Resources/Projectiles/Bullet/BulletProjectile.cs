using Godot;
using Godot.Collections;
using System;

public partial class BulletProjectile : Projectile
{
    [Export] public float fireForce;

    public override void InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint)
    {
        BulletProjectileBehaviour bullet = ProjectileScene.Instantiate<BulletProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation - Mathf.Pi / 2f;
        bullet.Stats = finalStats;
        bullet.BulletData = this;

        // Colors sprites under bullet instance
        Array<DamageType> damageTypes = [];
        foreach (TowerEffect effect in Effects) 
            damageTypes.Add(effect.damageType);
        foreach (Node node in bullet.GetChildren()) {
            if (node is Sprite2D sprite)
                sprite.SelfModulate = DamageTypeColor.GetMultipleDamageTypeColor(damageTypes);
        }

        firePoint.GetTree().Root.AddChild(bullet);
    }
}
