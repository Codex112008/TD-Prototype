using Godot;
using Godot.Collections;

[GlobalClass]
public partial class BuildingManager : Node2D
{
	public static BuildingManager instance;
	public override void _EnterTree()
	{
		instance = this;
	}

	[Export] public PackedScene TowerSelectionButtonScene;
	[Export] public HBoxContainer TowerSelectionButtonContainer;
	[Export] public Node InstancedNodesParent;
	[Export] private string _pathToSavedTowers = "res://RuntimeData/SavedTowers/";
	[Export] private Node _towerParent;
	[Export] private RichTextLabel _currentCurrencyLabel;

	public int _playerCurrency = 300;
	private PackedScene _selectedTower = null;
	private Tower _towerPreview = null;
	private Array<PackedScene> _towersToBuild = [];
	private bool _validTowerPlacement;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{

	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_selectedTower != null && IsInstanceValid(_towerPreview))
		{
			_towerPreview.GlobalPosition = _towerPreview.Position.Lerp(GetPreviewMousePosition(), 30f * (float)delta);
			if (!_validTowerPlacement)
				_towerPreview.Modulate = new Color("#ffa395");
			else
				_towerPreview.Modulate = Colors.White;

			_validTowerPlacement = PathfindingManager.instance.TilemapBuildableData[(Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize)] == true
							&& IsInstanceValid(_towerPreview)
							&& _towerPreview.GetFinalTowerStats()[TowerStat.Cost] <= _playerCurrency;
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

		_currentCurrencyLabel.Text = '$' + _playerCurrency.ToString();

		GenerateTowerSelectionButtons();
	}

	public override void _UnhandledInput(InputEvent @event)
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
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape && IsInstanceValid(_towerPreview))
			{
				_selectedTower = null;

				_towerPreview.QueueFree();
			}
		}
	}

	public Vector2I GetPreviewMousePosition()
	{
		return (Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize) * PathfindingManager.instance.TileSize;
	}

	private void BuildTower()
	{
		_selectedTower = null;

		_towerPreview.IsBuildingPreview = false;
		_towerPreview.Modulate = Colors.White;
		_playerCurrency -= Mathf.FloorToInt(_towerPreview.GetFinalTowerStats()[TowerStat.Cost]);
		_currentCurrencyLabel.Text = '$' + _playerCurrency.ToString();

		_towerPreview = null;
	}

	public void SetSelectedTower(int index = -1)
	{
		if (IsInstanceValid(_towerPreview))
			_towerPreview.QueueFree();

		if (index != -1)
		{
			if (_towersToBuild[index] != _selectedTower)
			{
				_selectedTower = _towersToBuild[index];
				_towerPreview = _selectedTower.Instantiate<Tower>();
				_towerPreview.IsBuildingPreview = true;
				_towerPreview.GlobalPosition = GetPreviewMousePosition();
				_towerParent.AddChild(_towerPreview);
			}
		}
		else
		{
			_selectedTower = null;
		}
	}

	public void GenerateTowerSelectionButtons()
	{
		foreach (Node child in TowerSelectionButtonContainer.GetChildren())
			child.QueueFree();

		for (int i = 0; i < _towersToBuild.Count; i++)
		{
			Tower tempTower = _towersToBuild[i].Instantiate<Tower>();

			TextureButton towerSelectionButton = TowerSelectionButtonScene.Instantiate<TextureButton>();
			int index = i;
			towerSelectionButton.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SetSelectedTower(index)));

			// Change button textures to saved tower icon
			Texture2D towerIcon = ImageTexture.CreateFromImage(Image.LoadFromFile(ProjectSettings.GlobalizePath(_towersToBuild[i].ResourcePath[.._towersToBuild[i].ResourcePath.LastIndexOf('.')] + "Icon.png")));
			towerSelectionButton.TextureNormal = towerIcon;
			towerSelectionButton.TexturePressed = towerIcon;

			RichTextLabel costLabel = towerSelectionButton.GetChild<RichTextLabel>(0);
			costLabel.Text = '$' + Mathf.FloorToInt(tempTower.GetFinalTowerStats()[TowerStat.Cost]).ToString();
			if (costLabel.Size.X > towerSelectionButton.Size.X)
				towerSelectionButton.Size = new(costLabel.Size.X, 0);

			TowerSelectionButtonContainer.AddChild(towerSelectionButton);

			tempTower.QueueFree();
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
		_playerCurrency += amount;
		_currentCurrencyLabel.Text = '$' + _playerCurrency.ToString();
	}
}
