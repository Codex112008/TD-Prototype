using Godot;
using Godot.Collections;
using System.Linq;

public partial class PenatratorProjectile : Projectile
{
    [Export] public float FireForce;
    [Export] public int Pierce;

    public override PenatratorProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint)
    {
        PenatratorProjectileBehaviour bullet = ProjectileScene.Instantiate<PenatratorProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = tower.GetFinalTowerStats();
        bullet.PenatratorData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        tower.InstancedProjectiles.AddChild(bullet);

        return bullet;
    }
}
