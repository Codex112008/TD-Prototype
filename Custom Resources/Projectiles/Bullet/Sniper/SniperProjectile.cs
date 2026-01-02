using Godot;
using Godot.Collections;
using System.Linq;

public partial class SniperProjectile : Projectile
{
    public override SniperProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        SniperProjectileBehaviour projectile = ProjectileScene.Instantiate<SniperProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = tower.GetFinalTowerStats();
        projectile.SniperData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        tower.InstancedProjectiles.AddChild(projectile);

        return projectile;
    }
}
