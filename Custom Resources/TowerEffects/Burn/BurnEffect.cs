using Godot;
using Godot.Collections;
using System;

public partial class BurnEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Burn, _finalStats[TowerStat.FireRate] * 2f);
    }
}
