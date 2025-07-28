using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public partial class Tower : Resource
{
    private Projectile _projectile;
    private bool _createdProjectileInstance = false;
    [Export]
    public Projectile Projectile
    {
        get => _projectile;
        set
        {
            // Always duplicate when setting a new projectile to not modify original projectile resource
            if (value != null && !_createdProjectileInstance)
            {
                _projectile = (Projectile)value.Duplicate();
                _createdProjectileInstance = true;
            }
            else
                _projectile = value;
        }
    }
    [Export] protected int Cost;
    [Export] public Dictionary<Stat, int> TowerStats;

    // Following functions used in tower creation
    public void SetEffects(Array<Effect> effects)
    {
        Projectile.Effects = effects;
    }

    public bool IsPointAllocationValid()
    {
        int totalPointsAllocated = Projectile.Effects.Sum(effect => effect.PointCost) + Projectile.PointCost + GetPointCostFromStats();
        if (totalPointsAllocated <= GetMaximumPointsFromCost())
            return true;
        else
            return false;
    }

    protected virtual int GetPointCostFromStats()
    {
        return TowerStats[Stat.Damage] + (TowerStats[Stat.Range] / 10) + (TowerStats[Stat.FireRate] * 2);
    }

    protected virtual int GetMaximumPointsFromCost()
    {
        return Cost / 50;
    }

    // Calculates the stats of the tower after multipliers from effect and projectile
    public Dictionary<Stat, float> GetFinalTowerStats()
    {
        Dictionary<Stat, float> finalTowerStats = [];
        foreach ((Stat stat, int value) in TowerStats)
        {
            finalTowerStats[stat] = value;
            finalTowerStats[stat] *= Projectile.StatMultipliers[stat];
            foreach (Effect effect in Projectile.Effects)
                finalTowerStats[stat] *= effect.StatMultipliers[stat];
        }
        return finalTowerStats;
    }
}

public enum Stat
{
    Damage,
    Range,
    FireRate,
}