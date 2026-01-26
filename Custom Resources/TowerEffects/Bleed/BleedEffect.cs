using Godot;
using Godot.Collections;
using System;

public partial class BleedEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Bleed, Mathf.Max(-0.5f + 2.1f * (float)Math.Log10(14 + target.CurrentEnemyStats[EnemyStat.Speed]), 0.01f) * towerStats[TowerStat.Damage]);
    }
}
