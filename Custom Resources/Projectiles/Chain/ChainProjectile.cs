using Godot;
using Godot.Collections;
using System.Linq;

public partial class ChainProjectile : Projectile
{
    public override ChainProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint)
    {
        ChainProjectileBehaviour projectile = ProjectileScene.Instantiate<ChainProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = finalStats;
        projectile.ChainData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.damageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);

        return projectile;
    }

    public ChainProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Vector2 firePos, float rotation, Array<Enemy> chainedEnemies)
    {
        ChainProjectileBehaviour projectile = ProjectileScene.Instantiate<ChainProjectileBehaviour>();
        projectile.GlobalPosition = firePos;
        projectile.Rotation = rotation;
        projectile.Stats = finalStats;
        projectile.ChainData = this;
        projectile.ChainedEnemies = chainedEnemies;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.damageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);

        return projectile;
    }
}
