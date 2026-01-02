using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class KatanaProjectile : Projectile
{
    [Export] public float WaveMaxSpeed = 600f;
    [Export] public float WaveInitialSpeed = 50f;
    [Export] public int Pierce = 3;

    public override KatanaProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        KatanaProjectileBehaviour sword = ProjectileScene.Instantiate<KatanaProjectileBehaviour>();
        sword.GlobalPosition = firePoint.GlobalPosition;
        sword.Rotation = firePoint.GlobalRotation;
        sword.Stats = tower.GetFinalTowerStats();
        sword.KatanaData = this;

        sword.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        tower.InstancedProjectiles.AddChild(sword);

        return sword;
    }
}
