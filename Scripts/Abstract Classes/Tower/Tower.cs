using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public abstract partial class Tower : Sprite2D
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
    [Export] public string TowerName;
    public float SellPercentage = 0.8f;
    public Node InstancedProjectiles;

    private Sprite2D _rangeOverlay;
    private TowerSelectedUI _selectedUI;
    private bool _inDisrepair = false;

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
        if (DoesUpgradeExist())
        {
            Tuple<string, int> towerPathAndLevel = Utils.TrimNumbersFromString(SceneFilePath[..SceneFilePath.LastIndexOf('.')]);
            Tower upgradedTower = GD.Load<PackedScene>(towerPathAndLevel.Item1 + (towerPathAndLevel.Item2 + 1) + ".tscn").Instantiate<Tower>();
            _selectedUI.UpgradeButton.Text = "Upgrade: $" + Mathf.FloorToInt(upgradedTower.GetFinalTowerStats()[TowerStat.Cost] - GetFinalTowerStats()[TowerStat.Cost]);
            upgradedTower.QueueFree();
        }
        else
            _selectedUI.UpgradeButton.Text = "Create an upgrade!";
            
        AddChild(_selectedUI);
        _selectedUI.Position = rectSize;

        if (!IsInstanceValid(InstancedProjectiles))
        {
            InstancedProjectiles = new Node()
            {
                Name = "Projectiles"
            };
            AddChild(InstancedProjectiles);
        }
    }

    public override void _Process(double delta)
    {
        if (_inDisrepair)
            Modulate = new Color("#393f47");

        if (!_inDisrepair && !FileAccess.FileExists(SceneFilePath) && !RangeAlwaysVisible)
            _inDisrepair = true;

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

                if (_inDisrepair) // Changes button texts if doesnt meet req to sell
                {
                    _selectedUI.SellButton.Text = "Cant Sell!";
                    _selectedUI.UpgradeButton.Text = "Cant Upgrade!";
                }
            }
        }
        else
        {
            if (_rangeOverlay.Scale == Vector2.Zero && _rangeOverlay.Visible != false)
                _rangeOverlay.Visible = false;
            _rangeOverlay.Scale = _rangeOverlay.Scale.Lerp(new Vector2(0.001f, 0.001f), 20f * (float)delta);

            if (_selectedUI.Scale == Vector2.Zero && _selectedUI.Visible != false)
                _selectedUI.Visible = false;
            _selectedUI.Scale = _selectedUI.Scale.Lerp(new Vector2(0.001f, 0.001f), 20f * (float)delta);
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

            if (mousePos == (Vector2I)GlobalPosition && !_inDisrepair)
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

    public Dictionary<string, Variant> SavePosition()
    {
        return new Dictionary<string, Variant>()
        {
            {"SceneFilePath", SceneFilePath}, // Replace this with json file path
            {"Parent", GetParent().GetPath()},
            {"PosX", (int)Position.X},
            {"PosY", (int)Position.Y},
        };
    }

    public void LoadPosition(Dictionary<string, Variant> saveData)
    {
        Position = new Vector2I((int)saveData["PosX"], (int)saveData["PosY"]);
    }

    /* 
		Save
		- Tower type
		- Tower stats
		- Tower projectile
		- Tower effects
		- Tower name
    public Dictionary<string, Variant> SaveTowerData()
    {
        return new Dictionary<string, Variant>()
        {
            {"Type", GetType().Name},
            {"BaseStats", BaseTowerStats},
            {"Projectile", Projectile},
            {"Name", TowerName}
        };
    }
    */

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
            finalTowerStats[stat] = value * Projectile.StatMultipliers[stat];
            
            if (stat != TowerStat.Damage) // Only damage multiplier individually applied per effect, so other multipliers are on tower
            {
                foreach (TowerEffect effect in Projectile.Effects)
                    finalTowerStats[stat] *= effect.StatMultipliers[stat];

                if (stat == TowerStat.Range && _inDisrepair)
                    finalTowerStats[stat] *= 0f;
            }
        }
        return finalTowerStats;
    }

    public void Upgrade()
    {
        if (!_inDisrepair) 
        {
            if (DoesUpgradeExist())
            {
                // Spawn upgraded tower
                Tuple<string, int> towerPathAndLevel = Utils.TrimNumbersFromString(SceneFilePath[..SceneFilePath.LastIndexOf('.')]);
                Tower upgradedTower = GD.Load<PackedScene>(towerPathAndLevel.Item1 + (towerPathAndLevel.Item2 + 1) + ".tscn").Instantiate<Tower>();
                if (Mathf.FloorToInt(upgradedTower.GetFinalTowerStats()[TowerStat.Cost] - GetFinalTowerStats()[TowerStat.Cost]) <= BuildingManager.instance.PlayerCurrency)
                {
                    upgradedTower.GlobalPosition = GlobalPosition;
                    BuildingManager.instance.TowerParent.AddChild(upgradedTower);

                    // Deduct player currency by difference in cost stats
                    BuildingManager.instance.AddPlayerCurrency(Mathf.FloorToInt(GetFinalTowerStats()[TowerStat.Cost] - upgradedTower.GetFinalTowerStats()[TowerStat.Cost]));

                    // Delete current tower
                    QueueFree();
                }
                else
                {
                    GetTree().CreateTimer(1f).Connect(Timer.SignalName.Timeout, Callable.From(_selectedUI.ResetUpgradeButtonText));
                    _selectedUI.UpgradeButton.Text = "Need Money!";
                    upgradedTower.QueueFree();
                }
            }
            else // If no upgrade exists send to upgrade creator
                RunController.instance.SwapScene(RunController.instance.TowerCreationScene, Key.W, GD.Load<PackedScene>(SceneFilePath));
        }
    }

    public void Sell()
    {
        if (!_inDisrepair)
        {
            // Give back some amount of money
            BuildingManager.instance.AddPlayerCurrency(Mathf.FloorToInt(GetFinalTowerStats()[TowerStat.Cost] * SellPercentage));

            // Make tile buildable again
            PathfindingManager.instance.TilemapBuildableData[(Vector2I)(GlobalPosition / PathfindingManager.instance.TileSize)] = true;

            // Delete current tower
            QueueFree();
        }
    }

    protected bool DoesUpgradeExist()
    {
        Tuple<string, int> towerPathAndLevel = Utils.TrimNumbersFromString(SceneFilePath[..SceneFilePath.LastIndexOf('.')]);
        return ResourceLoader.Exists(towerPathAndLevel.Item1 + (towerPathAndLevel.Item2 + 1) + ".tscn");
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
        return ConvertTowerRangeToTiles(GetFinalTowerStats()[TowerStat.Range]);
    }

    public static float ConvertTowerRangeToTiles(float rangeStat)
    {
        if (IsInstanceValid(PathfindingManager.instance.LevelTilemap))
            return rangeStat * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f;
        else
            return rangeStat * 3.2f; // shouldnt do anything cuz only nulls if im swapping scenes where its irrelevant
    }

    protected Vector2 GetCenteredGlobalPosition()
    {
        return GlobalPosition + PathfindingManager.instance.LevelTilemap.TileSet.TileSize / 2;
    }

    protected bool VectorInRange(Vector2 pos)
    {
        return (GlobalPosition + (Vector2.One * 8f)).DistanceTo(pos) <= GetRangeInTiles();
    }

    // Placeholder, change to actual targetting system later
    protected CharacterBody2D FindFirstEnemy()
    {
        Enemy firstEnemy = null;
        foreach (Node node in GetTree().GetNodesInGroup("Enemy"))
        {
            if (node is Enemy enemy)
            {
                if (((firstEnemy == null) || enemy.PathArray.Count < firstEnemy.PathArray.Count && enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro) >= firstEnemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro)) && VectorInRange(enemy.GlobalPosition))
                {
                    firstEnemy = enemy;
                }
            }
        }

        if (firstEnemy == null && !Projectile.RequireEnemy)
        {
            Array<Vector2I> walkableTilesInRange = GetWalkableTileInRange();
            if (walkableTilesInRange.Count > 0)
            {
                RandomNumberGenerator rand = new();
                Vector2 randomPos;
                do
                    randomPos = PathfindingManager.instance.GetTileToGlobalPos(walkableTilesInRange[rand.RandiRange(0, walkableTilesInRange.Count - 1)]) + new Vector2(rand.RandfRange(6f, 10f), rand.RandfRange(6f, 10f));
                while(PathfindingManager.instance.IsTileAtGlobalPosSolid(randomPos));

                CharacterBody2D dummyBody = new();
                AddChild(dummyBody);
                dummyBody.GlobalPosition = randomPos;
                return dummyBody;
            }
        }

        return firstEnemy;
    }

    private Array<Vector2I> GetWalkableTileInRange()
    {
        TileMapLayer tilemap = PathfindingManager.instance.LevelTilemap;
        Array<Vector2I> tilePosArray = [.. tilemap.GetUsedCells().Select(tile => PathfindingManager.instance.GlobalToCenteredGlobalTilePos(PathfindingManager.instance.GetTileToGlobalPos(tile))).Where(VectorInRange).Select(PathfindingManager.instance.GlobalToTilePos).Where(tile => (int)tilemap.GetCellTileData(tile).GetCustomData("MovementCost") < 10)];
        return tilePosArray;
    }

    protected abstract void Fire();
}