using Godot;
using Godot.Collections;
using System;

public partial class BleedEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Bleed, _finalStats[TowerStat.Damage]);
    }
}
