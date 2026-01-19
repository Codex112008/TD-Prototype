using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class LuckyEffect : TowerEffect
{
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target)
    {
        RandomNumberGenerator rand = new();
        if (rand.Randf() <= (0.005f + Mathf.Pow(1.1f, -2.2f * (towerStats[TowerStat.FireRate] - Mathf.E)) / 7.5f))
        {
            BuildingManager.instance.AddPlayerCurrency(Mathf.RoundToInt(target.CurrentEnemyStats[EnemyStat.DeathReward] / 10f));
            target.TakeDamage(towerStats[TowerStat.Damage] * 2, DamageType);
        }
        else
            target.TakeDamage(towerStats[TowerStat.Damage] * 0.7f, DamageType);
    }
}
