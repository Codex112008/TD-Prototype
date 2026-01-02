using Godot;
using Godot.Collections;
using System.Linq;

public partial class DroneProjectile : Projectile
{
	[Export] public float FireSpeed = 400f;
    [Export] public int MaxSpawns = 3;
    [Export] public float SummonSpeed = 30f;
    [Export] public float SummonAcceleration = 5f;

    public override DroneProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        if (IsInstanceValid(tower.InstancedProjectiles) && tower.InstancedProjectiles.GetChildCount() < MaxSpawns)
        {
            DroneProjectileBehaviour drone = ProjectileScene.Instantiate<DroneProjectileBehaviour>();
            drone.GlobalPosition = firePoint.GlobalPosition;
            drone.Rotation = firePoint.GlobalRotation;
            drone.Stats = tower.GetFinalTowerStats();
            drone.DroneData = this;

            drone.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

            if (IsInstanceValid(tower.InstancedProjectiles))
                tower.InstancedProjectiles.AddChild(drone);

            return drone;
        }
        else
            return null;
    }
}
