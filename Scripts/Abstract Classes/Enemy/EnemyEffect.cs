using Godot;
using System;

public abstract partial class EnemyEffect : Resource
{
    /// <summary>
    /// Should be set in case it a EnemyEffectTrigger with this effect is set to OnTimer
    /// </summary>
    [Export] public float EffectInterval = 999;

    public abstract void ApplyEffect();
}