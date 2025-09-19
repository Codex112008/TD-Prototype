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
	[Export] public HBoxContainer TowerSelectionButtonContainer;
	[Export] public Node InstancedNodesParent;
	[Export] private string _pathToSavedTowers = "RuntimeData/SavedTowers/";
	[Export] private Node _towerParent;
	[Export] private RichTextLabel _currentCurrencyLabel;
	[Export] private int _startingTowerSlots = 2;

	public Tower TowerPreview = null;
	public int PlayerCurrency = 300;
	private PackedScene _selectedTower = null;
	private Array<PackedScene> _towersToBuild = [];
	private bool _validTowerPlacement;
	private int _currentTowerSlots;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_pathToSavedTowers = OS.HasFeature("editor") ? "res://" + _pathToSavedTowers : "user://" + _pathToSavedTowers;
		if (!DirAccess.DirExistsAbsolute(_pathToSavedTowers))
			DirAccess.MakeDirRecursiveAbsolute(_pathToSavedTowers);
		
		GenerateTowerSelectionButtons();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_selectedTower != null && IsInstanceValid(TowerPreview))
		{
			TowerPreview.GlobalPosition = TowerPreview.Position.Lerp(GetPreviewMousePosition(), 30f * (float)delta);
			if (!_validTowerPlacement)
				TowerPreview.Modulate = new Color("#ffa395");
			else
				TowerPreview.Modulate = Colors.White;

			_validTowerPlacement = PathfindingManager.instance.TilemapBuildableData[(Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize)] == true
								   && IsInstanceValid(TowerPreview)
								   && Mathf.FloorToInt(TowerPreview.GetFinalTowerStats()[TowerStat.Cost]) <= PlayerCurrency;
		}
	}

	public void Init()
	{
		Array<string> savedTowers = GetFolderNames(_pathToSavedTowers);
		_towersToBuild.Clear();
		for (int i = 0; i < savedTowers.Count; i++)
		{
			_towersToBuild.Add(GD.Load<PackedScene>(_pathToSavedTowers + savedTowers[i] + "/" + savedTowers[i] + ".tscn"));
		}

		_currentCurrencyLabel.Text = '$' + PlayerCurrency.ToString();

		UpdateTowerSelectionButtons();
	}

	public override void _Input(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed == true)
			{
				if (_validTowerPlacement)
				{
					PathfindingManager.instance.TilemapBuildableData[(Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize)] = false;

					BuildTower();
				}
			}
		}

		if (@event is InputEventKey eventKey)
		{
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape && IsInstanceValid(TowerPreview))
			{
				_selectedTower = null;

				TowerPreview.QueueFree();
			}
		}
	}

	public Vector2I GetPreviewMousePosition()
	{
		return (Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize) * PathfindingManager.instance.TileSize;
	}

	private void BuildTower()
	{
		if (IsInstanceValid(TowerPreview))
		{
			_selectedTower = null;

			TowerPreview.GlobalPosition = GetPreviewMousePosition();
			TowerPreview.IsBuildingPreview = false;
			TowerPreview.Modulate = Colors.White;
			PlayerCurrency -= Mathf.FloorToInt(TowerPreview.GetFinalTowerStats()[TowerStat.Cost]);
			_currentCurrencyLabel.Text = '$' + PlayerCurrency.ToString();

			TowerPreview = null;
		}
	}

	public void SetSelectedTower(int index = -1)
	{
		if (IsInstanceValid(TowerPreview))
			TowerPreview.QueueFree();

		if (index >= _towersToBuild.Count || index == -1)
			_selectedTower = null;
		else if (_towersToBuild[index] != _selectedTower)
		{
			_selectedTower = _towersToBuild[index];
			TowerPreview = _selectedTower.Instantiate<Tower>();
			TowerPreview.IsBuildingPreview = true;
			TowerPreview.GlobalPosition = GetPreviewMousePosition();
			_towerParent.AddChild(TowerPreview);
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
				if (i < _towersToBuild.Count)
				{
					Tower tempTower = _towersToBuild[i].Instantiate<Tower>();
					RichTextLabel costLabel = towerSelectionButton.GetChild<RichTextLabel>(0);

					// Change button textures to saved tower icon
					string iconFilePath = _towersToBuild[i].ResourcePath[6.._towersToBuild[i].ResourcePath.LastIndexOf('.')] + "Icon.png";
					iconFilePath = OS.HasFeature("editor") ? "res://" + iconFilePath : "user://" + iconFilePath;
					Texture2D towerIcon = ImageTexture.CreateFromImage(Image.LoadFromFile(iconFilePath));
					towerSelectionButton.TextureNormal = towerIcon;
					towerSelectionButton.TexturePressed = towerIcon;
					towerSelectionButton.TextureHover = towerIcon;

					// Set cost label text
					costLabel.Text = '$' + Mathf.FloorToInt(tempTower.GetFinalTowerStats()[TowerStat.Cost]).ToString();
					if (costLabel.Size.X > towerSelectionButton.Size.X)
						towerSelectionButton.Size = new(costLabel.Size.X, 0);

					towerSelectionButton.TooltipText = tempTower.TowerName;

					tempTower.QueueFree();
				}
				else
				{
					towerSelectionButton.TextureNormal = null;
					towerSelectionButton.TexturePressed = null;
					towerSelectionButton.TextureHover = null;
					towerSelectionButton.TooltipText = "Empty Slot! Click 'W' to make your tower!";
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
		return EnemyManager.instance.TowerSlotUnlockWave.Where(wave => wave <= EnemyManager.instance.CurrentWave).Count() + _startingTowerSlots;
	}
}
