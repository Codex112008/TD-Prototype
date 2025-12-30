using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class SwordProjectile : Projectile
{
    public override SwordProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        SwordProjectileBehaviour sword = ProjectileScene.Instantiate<SwordProjectileBehaviour>();
        sword.GlobalPosition = firePoint.GlobalPosition;
        sword.Rotation = firePoint.GlobalRotation;
        sword.Stats = towerStats;
        sword.SwordData = this;

        sword.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(sword);

        return sword;
    }
}
