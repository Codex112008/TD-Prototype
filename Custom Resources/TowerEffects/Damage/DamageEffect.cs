using Godot.Collections;

public partial class DamageEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        target.TakeDamage(stats[TowerStat.Damage], DamageType);
    }
}
