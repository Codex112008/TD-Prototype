using Godot;
using System;

[GlobalClass]
public abstract partial class EnemyEffect : Resource
{
    /// Should be set in case it a EnemyEffectTrigger with this effect is set to OnTimer
    [Export] public float EffectInterval = 999;

    /// Should be set in case it a EnemyEffectTrigger with this effect is set to OnThreshold
    [Export] public float HealthPercentageThreshold = 0;

    public abstract void ApplyEffect(Enemy enemy);
}