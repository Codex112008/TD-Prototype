using Godot;
using System;

public partial class RewardEffect : EnemyEffect
{
    public override void ApplyEffect(Enemy enemy)
    {
        BuildingManager.instance.AddPlayerCurrency(Mathf.RoundToInt(enemy.CurrentEnemyStats[EnemyStat.DeathReward]));
    }
}
