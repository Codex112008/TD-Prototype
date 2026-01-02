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
        KatanaProjectileBehaviour katana = ProjectileScene.Instantiate<KatanaProjectileBehaviour>();
        katana.GlobalPosition = firePoint.GlobalPosition;
        katana.Rotation = firePoint.GlobalRotation;
        katana.Stats = tower.GetFinalTowerStats();
        katana.KatanaData = this;

        katana.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(katana);

        return katana;
    }
}
