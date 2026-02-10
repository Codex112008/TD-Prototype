using Godot;
using Godot.Collections;
using System;

public partial class StunEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.AddStatusEffectStacks(StatusEffect.Stun, _finalStats[TowerStat.Damage] / 5f);
        target.TakeDamage(_finalStats[TowerStat.Damage], DamageType);

        if (target.PathArray.Count > 0)
        {
            Vector2 dir = target.GlobalPosition.DirectionTo(target.PathArray[0]).Normalized();
            target.Velocity -= dir * _finalStats[TowerStat.Damage] * 7.5f * Mathf.Pow(0.975f, target.CurrentEnemyStats[EnemyStat.Defence]);
        }
    }
}
