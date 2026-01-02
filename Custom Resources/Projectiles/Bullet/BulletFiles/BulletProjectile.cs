using Godot;
using Godot.Collections;
using System.Linq;

public partial class BulletProjectile : Projectile
{
    [Export] public float FireForce;

    public override BulletProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        BulletProjectileBehaviour bullet = ProjectileScene.Instantiate<BulletProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = tower.GetFinalTowerStats();
        bullet.BulletData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(bullet);

        return bullet;
    }
}
