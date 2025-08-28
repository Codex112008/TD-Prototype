using Godot;
using Godot.Collections;
using System.Linq;

public partial class SniperProjectile : Projectile
{
    public override void InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint)
    {
        SniperProjectileBehaviour projectile = ProjectileScene.Instantiate<SniperProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = finalStats;
        projectile.SniperData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.damageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);
    }
}
