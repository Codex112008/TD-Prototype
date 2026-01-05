using Godot;
using Godot.Collections;
using System;

public partial class NecromancyEffect : TowerEffect
{
    [Export] private PackedScene _remnantScene;
    [Export] private Color _remnantTint;
    [Export] private float _healthPercentageToConvert = 0.4f;
    [Export] private float _speedPercentageToConvert = 0.7f;
    [Export] public float RemnantAcceleration;
    [Export] public DamageEffect RemnantDamageEffect;

    protected override void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target)
    {
        float targetHealth = target.GetCurrentHealth();
        targetHealth -= target.TakeDamage(towerStats[TowerStat.Damage], DamageType);
        if (targetHealth <= 0f) // Ressurect the dead
        {
            RandomNumberGenerator rand = new();
            RemnantBehaviour remnant = _remnantScene.Instantiate<RemnantBehaviour>();
            remnant.MaxHealth = target.CurrentEnemyStats[EnemyStat.MaxHealth] * _healthPercentageToConvert;
            remnant.Speed = target.CurrentEnemyStats[EnemyStat.Speed] * _speedPercentageToConvert;
            remnant.NecromancyData = this;
            remnant.TargetPos = EnemyManager.instance.SpawnPoints[rand.RandiRange(0, EnemyManager.instance.SpawnPoints.Count - 1)];
            remnant.GlobalPosition = target.GlobalPosition;
            remnant.Sprite.Texture = target.Sprite.Texture;
            remnant.Modulate = _remnantTint;
            remnant.Sprite.Rotation = target.Sprite.Rotation + Mathf.Pi;
            BuildingManager.instance.TowerParent.CallDeferred(Node.MethodName.AddChild, remnant);
        }
    }
}
