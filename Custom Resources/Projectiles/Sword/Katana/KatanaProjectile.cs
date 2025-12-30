using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class KatanaProjectile : Projectile
{
    public override KatanaProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        KatanaProjectileBehaviour sword = ProjectileScene.Instantiate<KatanaProjectileBehaviour>();
        sword.GlobalPosition = firePoint.GlobalPosition;
        sword.Rotation = firePoint.GlobalRotation;
        sword.Stats = towerStats;
        sword.KatanaData = this;

        sword.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(sword);

        return sword;
    }
}
