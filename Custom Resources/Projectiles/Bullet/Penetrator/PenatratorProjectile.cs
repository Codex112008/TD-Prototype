using Godot;
using Godot.Collections;
using System.Linq;

public partial class PenatratorProjectile : Projectile
{
    [Export] public int Pierce;

    public override PenatratorProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        PenatratorProjectileBehaviour bullet = ProjectileScene.Instantiate<PenatratorProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = tower.GetFinalTowerStats();
        bullet.PenatratorData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(bullet);

        return bullet;
    }
}
