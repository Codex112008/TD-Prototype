using Godot;
using Godot.Collections;
using System;

public partial class DamageEffect : Effect
{
    public override void ApplyEffect(Dictionary<Stat, float> stats, Enemy target)
    {
        target.TakeDamage(stats[Stat.Damage]);
    }
}
