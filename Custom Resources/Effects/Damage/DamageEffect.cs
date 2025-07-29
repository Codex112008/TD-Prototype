using Godot;
using Godot.Collections;
using System;

public partial class DamageEffect : TowerEffect
{
    public override void ApplyEffect(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.TakeDamage(stats[TowerStat.Damage]);
    }
}
