using Godot;
using Godot.Collections;
using System;

public partial class StunEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Stun, _finalStats[TowerStat.Damage] * 5);
        target.TakeDamage(_finalStats[TowerStat.Damage], DamageType);
    }
}
