using Godot;
using Godot.Collections;
using System.Linq;

public partial class ChainProjectile : Projectile
{
    [Export] public float MaxChainDistance = 30f;

    public override ChainProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        ChainProjectileBehaviour projectile = ProjectileScene.Instantiate<ChainProjectileBehaviour>();
        projectile.GlobalPosition = firePoint.GlobalPosition;
        projectile.Rotation = firePoint.GlobalRotation;
        projectile.Stats = tower.GetFinalTowerStats();
        projectile.ChainData = this;

        // Colors the line
        projectile.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(projectile);

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
