using Godot;
using System;

[GlobalClass]
public partial class ApplyStatusEffect : EnemyEffect
{
    [Export] private StatusEffect _status;
    [Export] private float _stacks;
    
    public override void ApplyEffect(Enemy enemy)
    {
        enemy.AddStatusEffectStacks(_status, _stacks);
    }
}
