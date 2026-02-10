using Godot;
using Godot.Collections;
using System.Linq;

public partial class SpikeProjectile : Projectile
{
    [Export] public int MaxSpawns = 3;

    public override SpikeProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        if (IsInstanceValid(tower.InstancedProjectiles) && (tower.InstancedProjectiles.GetChildCount() < MaxSpawns || tower.GetTree().GetNodeCountInGroup("Enemy") > 0))
        {
            SpikeProjectileBehaviour spike = ProjectileScene.Instantiate<SpikeProjectileBehaviour>();
            spike.GlobalPosition = firePoint.GlobalPosition;
            spike.Rotation = firePoint.GlobalRotation;
            spike.Stats = tower.GetFinalTowerStats();
            spike.TargetPos = targetGlobalPos;
            spike.SpikeData = this;

            spike.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

            if (IsInstanceValid(tower.InstancedProjectiles))
                tower.InstancedProjectiles.AddChild(spike);

            return spike;
        }
        else
            return null;
    }
}
