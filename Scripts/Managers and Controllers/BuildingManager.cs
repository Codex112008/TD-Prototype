using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class BuildingManager : Node2D, IManager
{
	public static BuildingManager instance;
	public override void _EnterTree()
	{
		instance = this;
	}

	[Export] public PackedScene TowerSelectionButtonScene;
	[Export] public VBoxContainer TowerSelectionButtonContainer;
	[Export] public Node InstancedNodesParent;
	[Export] public Node TowerParent;
	[Export] private string _pathToSavedTowers = "RuntimeData/SavedTowers/";
	[Export] private RichTextLabel _currentCurrencyLabel;
	[Export] private RichTextLabel _currentHealthLabel;
	[Export] private int _startingTowerSlots = 2;
	[Export] private Texture2D _openTowerSlotIcon;

	public Tower TowerPreview = null;
	public int PlayerCurrency = 300;
	private int _playerHealth = 10;
	private PackedScene _selectedTower = null;
	private Array<PackedScene> _towersToBuild = [];
	private bool _validTowerPlacement;
	private int _currentTowerSlots;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_pathToSavedTowers = Utils.AddCorrectDirectoryToPath(_pathToSavedTowers);
		if (!DirAccess.DirExistsAbsolute(_pathToSavedTowers))
			DirAccess.MakeDirRecursiveAbsolute(_pathToSavedTowers);
		
		GenerateTowerSelectionButtons();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (IsInstanceValid(TowerPreview))
		{
			Vector2I mousePos = PathfindingManager.instance.GetMouseTilemapPos();
			Dictionary<Vector2I, bool> tilemapBuildableData = PathfindingManager.instance.TilemapBuildableData;

			TowerPreview.GlobalPosition = TowerPreview.Position.Lerp(PathfindingManager.instance.GetMouseGlobalTilemapPos(), 30f * (float)delta);
			TileData targetTile = PathfindingManager.instance.LevelTilemap.GetCellTileData(mousePos);
			if (targetTile == null || (tilemapBuildableData[mousePos] == false && (int)targetTile.GetCustomData("MovementCost") > 10))
				TowerPreview.Modulate = TowerPreview.Modulate.Lerp(Colors.Transparent, 20f * (float)delta);
            else
            {
				if (!_validTowerPlacement)
					TowerPreview.Modulate = new Color("#ffa395");
				else
					TowerPreview.Modulate = Colors.White;
			}

			_validTowerPlacement = tilemapBuildableData.ContainsKey(mousePos) == true
								   && tilemapBuildableData[mousePos] == true
								   && /*IsInstanceValid(TowerPreview)*/ TowerPreview != null
								   && Mathf.FloorToInt(TowerPreview.GetFinalTowerStats()[TowerStat.Cost]) <= PlayerCurrency;
		}
    }

	public void Init()
	{
		Array<string> savedTowers = GetFolderNames(_pathToSavedTowers);
		_towersToBuild.Clear();
		for (int i = 0; i < savedTowers.Count; i++)
		{
			_towersToBuild.Add(GD.Load<PackedScene>(_pathToSavedTowers + savedTowers[i] + "/" + savedTowers[i] + "0.tscn"));
		}

		_currentCurrencyLabel.Text = '$' + PlayerCurrency.ToString();
		_currentHealthLabel.Text = _playerHealth.ToString();

		UpdateTowerSelectionButtons();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed == true)
		{
			if (_validTowerPlacement)
			{
				PathfindingManager.instance.TilemapBuildableData[PathfindingManager.instance.GetMouseTilemapPos()] = false;

				BuildTower();
			}
		}

		if (@event is InputEventKey eventKey && eventKey.Pressed)
		{
			if (eventKey.Keycode == Key.Escape)
			{
				SetSelectedTower();
			}
			else if (eventKey.Keycode == Key.Backspace)
			{
				DeleteSelectedTowerFromFilesystem();
			}
		}
	}

	private void BuildTower()
	{
		if (IsInstanceValid(TowerPreview))
		{
			_selectedTower = null;
			_sameTowerSelectedCounter = 0;

			TowerPreview.GlobalPosition = PathfindingManager.instance.GetMouseGlobalTilemapPos();
			TowerPreview.IsBuildingPreview = false;
			TowerPreview.Modulate = Colors.White;
			AddPlayerCurrency(-Mathf.FloorToInt(TowerPreview.GetFinalTowerStats()[TowerStat.Cost]));
			
			Tower.SelectedTower = null;

			TowerPreview = null;
		}
	}

	private int _sameTowerSelectedCounter = 0;
	public void SetSelectedTower(int index = -1)
	{
		if (IsInstanceValid(TowerPreview))
            TowerPreview.QueueFree();

		if (index >= _towersToBuild.Count || index == -1)
        {
            _selectedTower = null;
			_sameTowerSelectedCounter = 0;
        }
		else if (_towersToBuild[index] != _selectedTower)
		{
			_selectedTower = _towersToBuild[index];
			TowerPreview = _selectedTower.Instantiate<Tower>();
			TowerPreview.IsBuildingPreview = true;
			TowerPreview.GlobalPosition = PathfindingManager.instance.GetMouseGlobalTilemapPos();
			TowerParent.AddChild(TowerPreview);

			_sameTowerSelectedCounter = 0;
		}
        else // Same tower selected again
        {
            _sameTowerSelectedCounter++;
			if (_sameTowerSelectedCounter >= 2)
            {
				_sameTowerSelectedCounter = 0;

				RunController.instance.SwapScene(RunController.instance.TowerUpgradeTreeViewerScene, Key.D, GetSelectedTower());
            }
        }
	}

	public PackedScene GetSelectedTower()
	{
		return _selectedTower;
	}

	public void UpdateTowerSelectionButtons()
	{
		for (int i = 0; i < GetTotalTowerSlots(); i++)
		{
			TextureButton towerSelectionButton = TowerSelectionButtonContainer.GetChild<TextureButton>(i);
			if (i < GetOpenTowerSlots())
			{
				RichTextLabel costLabel = towerSelectionButton.GetChild<RichTextLabel>(0);

				if (i < _towersToBuild.Count)
				{
					Tower tempTower = _towersToBuild[i].Instantiate<Tower>();
					
					// Change button textures to saved tower icon
					string iconFilePath = _towersToBuild[i].ResourcePath[6.._towersToBuild[i].ResourcePath.LastIndexOf('.')] + "Icon.png";
					iconFilePath = Utils.AddCorrectDirectoryToPath(iconFilePath);
					Texture2D towerIcon = ImageTexture.CreateFromImage(Image.LoadFromFile(iconFilePath));
					towerSelectionButton.TextureNormal = towerIcon;
					towerSelectionButton.TexturePressed = towerIcon;
					towerSelectionButton.TextureHover = towerIcon;

					// Set cost label text
					costLabel.Text = '$' + Mathf.FloorToInt(tempTower.GetFinalTowerStats()[TowerStat.Cost]).ToString();
					if (costLabel.Size.X > towerSelectionButton.Size.X)
						towerSelectionButton.Size = new(costLabel.Size.X, 0);

					towerSelectionButton.TooltipText = tempTower.TowerName + " - Select and click 'W' to design upgrade, 'Backspace' to delete\nWARNING: Deleting leaves unsellable instances of tower";

					tempTower.QueueFree();
				}
				else
				{
					towerSelectionButton.TextureNormal = _openTowerSlotIcon;
					towerSelectionButton.TexturePressed = _openTowerSlotIcon;
					towerSelectionButton.TextureHover = _openTowerSlotIcon;
					towerSelectionButton.TooltipText = "Empty Slot! Click 'W' to make your tower!";

					costLabel.Text = "";
				}

				towerSelectionButton.Disabled = false;
			}
			else
			{
				towerSelectionButton.TooltipText = "This tower slot is locked! Unlocks at wave " + EnemyManager.instance.TowerSlotUnlockWave[i - _startingTowerSlots];
				towerSelectionButton.Disabled = true;
			}
		}
	}

	public static Array<string> GetFolderNames(string path)
	{
		// Get all directories at the specified path
		string[] folders = DirAccess.GetDirectoriesAt(path);

		if (folders == null)
		{
			GD.PushError($"Failed to access directory: {path}");
			return [];
		}

		return [.. folders];
	}

	public void AddPlayerCurrency(int amount)
	{
		PlayerCurrency += amount;
		_currentCurrencyLabel.Text = '$' + PlayerCurrency.ToString();

		/*
		_playerCurrencyTween.Kill();
		_playerCurrencyTween = CreateTween();
		_playerCurrencyTween.TweenProperty(this, "_playerCurrencyToDisplay", PlayerCurrency, 1f).SetTrans(Tween.TransitionType.Linear).SetEase(Tween.EaseType.In);
		*/
	}

	public void DeleteSelectedTowerFromFilesystem()
	{
		Utils.RemoveDirRecursive(_pathToSavedTowers + Utils.RemoveWhitespaces(TowerPreview.TowerName));
		SetSelectedTower();

		Init(); // Remake tower selection buttons
	}

	public void Deload()
	{
		instance = null;
	}

	public bool IsMaxTowersCreated()
	{
		return GetOpenTowerSlots() <= _towersToBuild.Count;
	}

	private void GenerateTowerSelectionButtons()
	{
		foreach (Node child in TowerSelectionButtonContainer.GetChildren())
			child.QueueFree();

		for (int i = 0; i < GetTotalTowerSlots(); i++)
		{
			TextureButton towerSelectionButton = TowerSelectionButtonScene.Instantiate<TextureButton>();
			int index = i;
			towerSelectionButton.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SetSelectedTower(index)));

			TowerSelectionButtonContainer.AddChild(towerSelectionButton);
		}
	}

	private int GetTotalTowerSlots()
	{
		return EnemyManager.instance.TowerSlotUnlockWave.Count + _startingTowerSlots;
	}

	private int GetOpenTowerSlots()
	{
		return EnemyManager.instance.TowerSlotUnlockWave.Count(wave => wave <= EnemyManager.instance.CurrentWave) + _startingTowerSlots;
	}

	public void TakeDamage(int damage)
	{
		_playerHealth -= damage;
		_currentHealthLabel.Text = _playerHealth.ToString();
		if (_playerHealth <= 0)
		{
			NewRunButton.DeleteExistingSave();
			GetTree().ChangeSceneToFile("res://Scenes/MainScenes/MainMenu.tscn");
		}
	}
}
