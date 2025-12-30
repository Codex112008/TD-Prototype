using Godot;
using Godot.Collections;
using System.Linq;

public partial class ChainProjectile : Projectile
{
    public override ChainProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        ChainProjectileBehaviour projectile = ProjectileScene.Instantiate<ChainProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = towerStats;
        projectile.ChainData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);

        return projectile;
    }

    public ChainProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Vector2 firePos, float rotation, Array<Enemy> chainedEnemies)
    {
        ChainProjectileBehaviour projectile = ProjectileScene.Instantiate<ChainProjectileBehaviour>();
        projectile.GlobalPosition = firePos;
        projectile.Rotation = rotation;
        projectile.Stats = towerStats;
        projectile.ChainData = this;
        projectile.ChainedEnemies = chainedEnemies;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(projectile);

        return projectile;
    }
}
