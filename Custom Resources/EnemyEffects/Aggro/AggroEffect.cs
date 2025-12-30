using Godot;
using System;

public partial class AggroEffect : EnemyEffect
{
    public override void ApplyEffect(Enemy enemy)
    {
        enemy.AddStatusEffectStacks(StatusEffect.Aggro, 1);
    }
}
