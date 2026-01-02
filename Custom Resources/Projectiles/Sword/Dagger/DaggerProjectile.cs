using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class DaggerProjectile : Projectile
{
    [Export] public float FireForce = 600f;

    public override DaggerProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        DaggerProjectileBehaviour dagger = ProjectileScene.Instantiate<DaggerProjectileBehaviour>();
        dagger.GlobalPosition = firePoint.GlobalPosition;
        dagger.Rotation = firePoint.GlobalRotation;
        dagger.Stats = tower.GetFinalTowerStats();
        dagger.DaggerData = this;

        dagger.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(dagger);

        return dagger;
    }
}
