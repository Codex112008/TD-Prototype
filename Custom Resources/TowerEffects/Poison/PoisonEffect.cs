using Godot;
using Godot.Collections;
using System;

public partial class PoisonEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Poison, _finalStats[TowerStat.Damage]);
        target.TakeDamage(_finalStats[TowerStat.Damage] * 0.2f, DamageType.Poison, true);
    }
}
