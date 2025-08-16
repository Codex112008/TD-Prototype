using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public abstract partial class Tower : Node2D
{
    private Projectile _projectile;
    private bool _createdProjectileInstance = false;
    [Export]
    public Projectile Projectile
    {
        get => _projectile;
        set
        {
            // Always duplicate when using in tower creator
            if (value != null)
            {
                _projectile = (Projectile)value.Duplicate();
                return;
            }

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
    [Export] public Dictionary<TowerStat, int> BaseTowerStats = [];

    public bool IsBuildingPreview = false;

    // Following functions used in tower creation
    public void SetEffects(Array<TowerEffect> effects)
    {
        if (Projectile != null)
            Projectile.Effects = effects;
    }

    public bool HasValidPointAllocation()
    {
        if (GetCurrentTotalPointsAllocated() <= GetMaximumPointsFromCost())
            return true;
        else
            return false;
    }

    public int GetCurrentTotalPointsAllocated()
    {
        return Projectile.Effects.Sum(effect => effect.PointCost) + Projectile.PointCost + GetPointCostFromStats();
    }

    protected virtual int GetPointCostFromStats()
    {
        return GetPointCostFromDamage() + GetPointCostFromRange() + GetPointCostFromFireRate();
    }

    public int GetPointCostForStat(TowerStat stat)
    {
        return stat switch
        {
            TowerStat.Cost => GetMaximumPointsFromCost(),
            TowerStat.Damage => GetPointCostFromDamage(),
            TowerStat.Range => GetPointCostFromRange(),
            TowerStat.FireRate => GetPointCostFromFireRate(),
            _ => throw new ArgumentOutOfRangeException($"Unhandled stat type: {stat}"),
        };

    }

    protected virtual int GetPointCostFromDamage()
    {
        return BaseTowerStats[TowerStat.Damage] * 50;
    }

    protected virtual int GetPointCostFromRange()
    {
        return BaseTowerStats[TowerStat.Range] * 5;
    }

    protected virtual int GetPointCostFromFireRate()
    {
        return BaseTowerStats[TowerStat.FireRate] * 100;
    }

    public virtual int GetMaximumPointsFromCost()
    {
        return BaseTowerStats[TowerStat.Cost];
    }

    // Calculates the stats of the tower after multipliers from effect and projectile
    public Dictionary<TowerStat, float> GetFinalTowerStats()
    {
        Dictionary<TowerStat, float> finalTowerStats = [];
        foreach ((TowerStat stat, int value) in BaseTowerStats)
        {
            finalTowerStats[stat] = value;
            finalTowerStats[stat] *= Projectile.StatMultipliers[stat];
            foreach (TowerEffect effect in Projectile.Effects)
                finalTowerStats[stat] *= effect.StatMultipliers[stat];
        }
        return finalTowerStats;
    }

    protected float GetRangeInTiles(){
        return PathfindingManager.instance.LevelTileMap.TileSet.TileSize / 10;
    }

    protected abstract void Fire();
}

public enum TowerStat
{
    Cost,
    Damage,
    Range,
    FireRate,
}