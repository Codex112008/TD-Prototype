using Godot;
using System;

public partial class RewardEffect : EnemyEffect
{
    public override void ApplyEffect(Enemy enemy)
    {
        BuildingManager.instance.AddPlayerCurrency(enemy.CurrentEnemyStats[EnemyStat.DeathReward]);
    }
}
