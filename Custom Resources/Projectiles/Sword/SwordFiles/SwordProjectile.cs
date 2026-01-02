using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class SwordProjectile : Projectile
{
    [Export] public int Pierce = 3;
    public override SwordProjectileBehaviour InstantiateProjectile(Tower tower, Marker2D firePoint, Vector2 targetGlobalPos)
    {
        SwordProjectileBehaviour sword = ProjectileScene.Instantiate<SwordProjectileBehaviour>();
        sword.GlobalPosition = firePoint.GlobalPosition;
        sword.Rotation = firePoint.GlobalRotation;
        sword.Stats = tower.GetFinalTowerStats();
        sword.SwordData = this;

        sword.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        if (IsInstanceValid(tower.InstancedProjectiles))
            tower.InstancedProjectiles.AddChild(sword);

        return sword;
    }
}
