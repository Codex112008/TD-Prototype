using Godot;
using System;
using System.Linq;

public partial class TowerUpgradeTree : Node2D
{
	public static TowerUpgradeTree instance;
	public override void _EnterTree()
	{
		instance = this;
	}

	[Export] private VBoxContainer _towerUpgradeTreeUI;
	[Export] private PackedScene _towerUpgradeDisplayScene;
	[Export] private TileMapLayer _towerPreviewArea;

	public string TowerPathToDisplay;

	private string _towerName;
	private int _maximumLevel;
	private Tower _towerPreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		// Gets name of tower as it is the name of the folder
		_towerName = TowerPathToDisplay[(TowerPathToDisplay.LastIndexOf('/') + 1)..];

		// Access the folder where tower data is stored
		DirAccess dirAccess = DirAccess.Open(TowerPathToDisplay);
		string currentDir = dirAccess.GetCurrentDir();

		// Each level of tower is saved with a corresponding icon file, halving amount of files in folders gives amount of upgrades
		_maximumLevel = dirAccess.GetFiles().Length / 2;
		for (int i = 0; i < _maximumLevel; i++)
        {
			string towerSceneFileName = _towerName + i + ".tscn";
            if (dirAccess.FileExists(towerSceneFileName))
            {
				// Create temporary instance of tower to load data from
                Tower tempTowerToDisplayUpgrade = GD.Load<PackedScene>(currentDir + '/' + towerSceneFileName).Instantiate<Tower>();
				TowerUpgradeDisplay towerUpgradeDisplay = _towerUpgradeDisplayScene.Instantiate<TowerUpgradeDisplay>();
				
				// Sets the upgrade display's tower to the temporary tower
				towerUpgradeDisplay.TowerToDisplayUpgrade = tempTowerToDisplayUpgrade;
				towerUpgradeDisplay.TowerLevel = i;

				// Add upgrade display to the scene tree
				_towerUpgradeTreeUI.AddChild(towerUpgradeDisplay);

				tempTowerToDisplayUpgrade.Free();
            }
        }

		Button addUpgradeButton = _towerUpgradeTreeUI.GetChild<Button>(0);
		_towerUpgradeTreeUI.MoveChild(addUpgradeButton, -1); // Moves the add upgrade button to the last index, so appears last in container

		ChangeTowerPreview(0);
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void ChangeTowerPreview(int towerLevelToDisplay)
    {
		_towerPreview?.QueueFree();
        _towerPreview = GD.Load<PackedScene>(TowerPathToDisplay + '/' + _towerName + towerLevelToDisplay + ".tscn").Instantiate<Tower>();
		_towerPreview.GlobalPosition = new Vector2I(11, 5) * PathfindingManager.instance.TileSize;
		_towerPreview.RangeAlwaysVisible = true;
		_towerPreviewArea.AddChild(_towerPreview);
    }

	public void OnAddUpgradeButtonClicked()
    {
        RunController.instance.SwapScene(RunController.instance.TowerCreationScene, Key.W, GD.Load<PackedScene>(TowerPathToDisplay + '/' + _towerName + (_maximumLevel - 1) + ".tscn"));
    }
}
