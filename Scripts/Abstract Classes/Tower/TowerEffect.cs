using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class TowerEffect : TowerComponent
{
    [Export] public DamageType DamageType = DamageType.Physical;

    protected Dictionary<TowerStat, float> _finalStats;
    
    public void ApplyEffect(Dictionary<TowerStat, float> towerStats, Enemy target)
    {
        ApplyEffectStatMultipliers(towerStats);
        ApplyEffectCore(towerStats, target);
    }

    // Example of an effect could be damage effect, so dealing damage SHOULD also be implemented as an effect
    protected abstract void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target);

    protected void ApplyEffectStatMultipliers(Dictionary<TowerStat, float> towerStats)
    {
        Dictionary<TowerStat, float> finalTowerStats = [];
        foreach ((TowerStat stat, float value) in towerStats)
        {
            finalTowerStats[stat] = value;
            finalTowerStats[stat] *= StatMultipliers[stat];
        }
        _finalStats = finalTowerStats;
    }
}
