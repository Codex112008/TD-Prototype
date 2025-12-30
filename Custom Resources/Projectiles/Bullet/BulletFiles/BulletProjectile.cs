using Godot;
using Godot.Collections;
using System.Linq;

public partial class BulletProjectile : Projectile
{
    [Export] public float FireForce;

    public override BulletProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        BulletProjectileBehaviour bullet = ProjectileScene.Instantiate<BulletProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = towerStats;
        bullet.BulletData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(bullet);

        return bullet;
    }
}
