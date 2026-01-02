using Godot;
using Godot.Collections;
using System.Linq;

public partial class DroneProjectile : Projectile
{
	[Export] public float FireSpeed = 400f;
    [Export] public int MaxSpawns = 3;
    [Export] public float SummonSpeed = 30f;
    [Export] public float SummonAcceleration = 5f;

    public override DroneProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint)
    {
        if (IsInstanceValid(tower.InstancedProjectiles) && tower.InstancedProjectiles.GetChildCount() < MaxSpawns)
        {
            DroneProjectileBehaviour bullet = ProjectileScene.Instantiate<DroneProjectileBehaviour>();
            bullet.GlobalPosition = firePoint.GlobalPosition;
            bullet.Rotation = firePoint.GlobalRotation;
            bullet.Stats = tower.GetFinalTowerStats();
            bullet.DroneData = this;

            bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

            tower.InstancedProjectiles.AddChild(bullet);

            return bullet;
        }
        else
            return null;
    }
}
