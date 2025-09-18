using Godot;
using Godot.Collections;
using System.Linq;

public partial class SniperProjectile : Projectile
{
    public override SniperProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        SniperProjectileBehaviour projectile = ProjectileScene.Instantiate<SniperProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = towerStats;
        projectile.SniperData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);

        return projectile;
    }
}
