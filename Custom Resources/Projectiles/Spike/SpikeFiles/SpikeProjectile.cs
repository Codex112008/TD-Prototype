using Godot;
using Godot.Collections;
using System.Linq;

public partial class SpikeProjectile : Projectile
{
	[Export] public float Speed = 400f;
    [Export] public int MaxSpawns = 3;

    public override SpikeProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        if (IsInstanceValid(tower.InstancedProjectiles) && tower.InstancedProjectiles.GetChildCount() < MaxSpawns)
        {
            SpikeProjectileBehaviour bullet = ProjectileScene.Instantiate<SpikeProjectileBehaviour>();
            bullet.GlobalPosition = firePoint.GlobalPosition;
            bullet.Rotation = firePoint.GlobalRotation;
            bullet.Stats = tower.GetFinalTowerStats();
            bullet.TargetPos = targetGlobalPos;
            bullet.SpikeData = this;

            bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

            tower.InstancedProjectiles.AddChild(bullet);

            return bullet;
        }
        else
            return null;
    }
}
