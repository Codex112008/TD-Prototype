using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public abstract partial class Tower : Sprite2D, ISavable
{
    public static Tower SelectedTower;
    [Export] public Dictionary<TowerStat, int> BaseTowerStats = new()
    {
        {TowerStat.Cost, 300},
        {TowerStat.Damage, 2},
        {TowerStat.Range, 25},
        {TowerStat.FireRate, 2}
    };
    [Export] public Array<Sprite2D> SpritesForIcon = [];
    [Export] public Array<Sprite2D> SpritesToColor = [];
    [Export(PropertyHint.MultilineText)] public string Tooltip;

    [Export] private Texture2D _rangeOverlayTexture;
    [Export] private PackedScene _towerSelectedUIScene;

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

    public bool IsBuildingPreview = false;
    public bool RangeAlwaysVisible = false;
<<<<<<< Updated upstream
    public string TowerName;
=======
    [Export] public string TowerName;
>>>>>>> Stashed changes
    public int TowerLevel = 0;

    private Sprite2D _rangeOverlay;
    private TowerSelectedUI _selectedUI;

    public override void _Ready()
    {
        SpritesForIcon.Insert(0, this);

        Vector2 rectSize = GetRect().Size;
        _rangeOverlay = new Sprite2D
        {
            Texture = _rangeOverlayTexture,
            Visible = false,
            Position = rectSize / 2,
            ZIndex = -1,
            SelfModulate = new Color(1f, 1f, 1f, 0.47f)
        };
        AddChild(_rangeOverlay);

        _selectedUI = _towerSelectedUIScene.Instantiate<TowerSelectedUI>();
        _selectedUI.Visible = false;
        _selectedUI.GetChild<RichTextLabel>(0).Text = TowerName;
        AddChild(_selectedUI);
        _selectedUI.Position = rectSize;
    }

    public override void _Process(double delta)
    {
        if (PathfindingManager.instance.GetMouseGlobalTilemapPos() == (Vector2I)GlobalPosition || RangeAlwaysVisible || IsBuildingPreview || SelectedTower == this)
        {
            if (_rangeOverlay.Visible == false)
                _rangeOverlay.Visible = true;
            _rangeOverlay.Scale = _rangeOverlay.Scale.Lerp(Vector2.One * (GetRangeInTiles() / 128f) * 2f, 12.5f * (float)delta);

            if (!RangeAlwaysVisible)
            {
                if (_selectedUI.Visible == false)
                    _selectedUI.Visible = true;
                _selectedUI.Scale = _selectedUI.Scale.Lerp(Vector2.One * 0.1f, 12.5f * (float)delta);
            }
        }
        else
        {
            if (_rangeOverlay.Scale == Vector2.Zero && _rangeOverlay.Visible != false)
                _rangeOverlay.Visible = false;
            _rangeOverlay.Scale = _rangeOverlay.Scale.Lerp(Vector2.Zero, 20f * (float)delta);

            if (_selectedUI.Scale == Vector2.Zero && _selectedUI.Visible != false)
                _selectedUI.Visible = false;
            _selectedUI.Scale = _selectedUI.Scale.Lerp(Vector2.Zero, 20f * (float)delta);
        }

        if (SelectedTower == this)
            _selectedUI.UpdateUpgradeUIVisibility(true);
        else
            _selectedUI.UpdateUpgradeUIVisibility(false);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed == true)
        {
            Vector2I mousePos = PathfindingManager.instance.GetMouseGlobalTilemapPos();

            if (mousePos == (Vector2I)GlobalPosition)
            {
                if (SelectedTower != this)
                    SelectedTower = this;
            }

            if (SelectedTower == this && mousePos != (Vector2I)GlobalPosition)
                SelectedTower = null;
        }

        if (@event is InputEventKey eventKey && eventKey.Pressed && eventKey.Keycode == Key.Escape)
        {
            if (SelectedTower == this)
            {
                SelectedTower = null;
                GetViewport().SetInputAsHandled();
            }
        }
    }

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

    public Dictionary<string, Variant> Save()
    {
        return new Dictionary<string, Variant>()
        {
            {"SceneFilePath", SceneFilePath},
            {"Parent", GetParent().GetPath()},
            {"PosX", (int)Position.X},
            {"PosY", (int)Position.Y},
        };
    }

    public void Load(Dictionary<string, Variant> saveData)
    {
        Position = new Vector2I((int)saveData["PosX"], (int)saveData["PosY"]);
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

    public void Upgrade()
    {
        // Spawn upgraded tower

        // Delete current tower
    }

    public void Sell()
    {
        // Give back some amount of money

        // Make tile buildable again
        PathfindingManager.instance.TilemapBuildableData[PathfindingManager.instance.GetMouseTilemapPos()] = true;

        // Delete current tower
    }

    protected virtual int GetPointCostFromStats()
    {
        return GetPointCostFromDamage() + GetPointCostFromRange() + GetPointCostFromFireRate();
    }

    protected virtual int GetPointCostFromDamage()
    {
        return BaseTowerStats[TowerStat.Damage] * 25;
    }

    protected virtual int GetPointCostFromRange()
    {
        return BaseTowerStats[TowerStat.Range] * 2;
    }

    protected virtual int GetPointCostFromFireRate()
    {
        return BaseTowerStats[TowerStat.FireRate] * 75;
    }

    public virtual int GetMaximumPointsFromCost()
    {
        return BaseTowerStats[TowerStat.Cost];
    }

    protected float GetRangeInTiles()
    {
        return GetFinalTowerStats()[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f;
    }

    protected Vector2 GetCenteredGlobalPosition()
    {
        return GlobalPosition + PathfindingManager.instance.LevelTilemap.TileSet.TileSize / 2;
    }

    protected abstract void Fire();
}