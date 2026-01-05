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
        ApplyEffectCore(_finalStats, target);
    }

    // Example of an effect could be damage effect, so dealing damage SHOULD also be implemented as an effect
    protected abstract void ApplyEffectCore(Dictionary<TowerStat, float> towerStats, Enemy target);

    protected void ApplyEffectStatMultipliers(Dictionary<TowerStat, float> towerStats)
    {
        Dictionary<TowerStat, float> finalTowerStats = [];
        foreach ((TowerStat stat, float value) in towerStats)
        {
            if (stat == TowerStat.Damage)
                finalTowerStats[stat] = value * StatMultipliers[stat]; // Damage mult is applied per effect not on tower
            else
                finalTowerStats[stat] = value;
        }
        _finalStats = finalTowerStats;
    }
}
