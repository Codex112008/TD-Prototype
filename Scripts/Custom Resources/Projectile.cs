using Godot;
using System;

public abstract partial class Projectile : Resource
{
    public Effect effect;

    public abstract void Fire();
}
