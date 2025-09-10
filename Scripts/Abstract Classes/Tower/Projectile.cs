using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Projectile : TowerComponent, ISavable
{
    [Export] protected PackedScene ProjectileScene;
    [Export] public Array<TowerEffect> Effects = [];

    // Fire the projectile, SHOULD instantiate ProjectileScene
    public abstract Node2D InstantiateProjectile(Dictionary<TowerStat, float> finalStats, Marker2D firePoint);

    public Dictionary<string, Variant> Save()
    {
        Dictionary<string, Variant> saveData = new() { { "ResourceResourcePath", ResourcePath } };

        // Add Effects to saveData
        Array<string> effectFilePaths = [];
        foreach (TowerEffect effect in Effects)
            effectFilePaths.Add(effect.ResourcePath);
        saveData.Add("EffectFilePaths", effectFilePaths);

        return saveData;
    }
    public void Load(Dictionary<string, Variant> saveData)
    {
        
    }
}
