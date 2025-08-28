using Godot;
using Godot.Collections;
using System.Linq;

public partial class BulletProjectile : Projectile
{
    [Export] public float fireForce;

    public override void InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint)
    {
        BulletProjectileBehaviour bullet = ProjectileScene.Instantiate<BulletProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = finalStats;
        bullet.BulletData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.damageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(bullet);
    }
}
