using Godot;
using Godot.Collections;
using System.Linq;

public partial class PenatratorProjectile : Projectile
{
    [Export] public float FireForce;
    [Export] public int Pierce;

    public override PenatratorProjectileBehaviour InstantiateProjectile(Dictionary<TowerStat, float> towerStats, Marker2D firePoint)
    {
        PenatratorProjectileBehaviour bullet = ProjectileScene.Instantiate<PenatratorProjectileBehaviour>();
        bullet.GlobalPosition = firePoint.GlobalPosition;
        bullet.Rotation = firePoint.GlobalRotation;
        bullet.Stats = towerStats;
        bullet.PenatratorData = this;

        bullet.Modulate = DamageTypeData.GetMultipleDamageTypeColor([.. Effects.Select(effect => effect.DamageType)]);

        BuildingManager.instance.InstancedNodesParent.AddChild(bullet);

        return bullet;
    }
}
