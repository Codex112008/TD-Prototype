using Godot;
using Godot.Collections;
using System;

public partial class ChillEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Chill, 1);
        target.TakeDamage(_finalStats[TowerStat.Damage], DamageType);
    }
}
