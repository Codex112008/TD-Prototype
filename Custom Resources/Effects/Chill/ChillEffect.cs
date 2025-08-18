using Godot;
using Godot.Collections;
using System;

public partial class ChillEffect : TowerEffect
{
    public override void ApplyEffect(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.ModifyStatusEffectStacks(StatusEffect.Chill, 1);
        target.TakeDamage(stats[TowerStat.Damage], damageType);
    }
}
