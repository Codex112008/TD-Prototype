using Godot;
using System;

public abstract partial class Tower : Resource
{
    public Projectile projectile;

    public virtual void SetEffect(Effect effect)
    {
        projectile.effect = effect;
    }
}
