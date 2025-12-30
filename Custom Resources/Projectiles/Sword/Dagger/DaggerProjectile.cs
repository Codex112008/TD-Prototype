using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class DaggerProjectile : Projectile
{
    [Export] public float FireForce = 600f;

    public override DaggerProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        DaggerProjectileBehaviour dagger = ProjectileScene.Instantiate<DaggerProjectileBehaviour>();
        dagger.GlobalPosition = firePoint.GlobalPosition;
        dagger.Rotation = firePoint.GlobalRotation;
        dagger.Stats = towerStats;
        dagger.DaggerData = this;

        dagger.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(dagger);

        return dagger;
    }
}
