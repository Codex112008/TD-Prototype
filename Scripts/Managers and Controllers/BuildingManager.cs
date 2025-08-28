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

	[Export] private string _pathToSavedTowers = "res://RuntimeData/SavedTowers/";
	[Export] public PackedScene TowerSelectionButtonScene;
	[Export] public HBoxContainer TowerSelectionButtonContainer;
	[Export] public Node InstancedNodesParent;
	[Export] private Node _towerParent;
	private PackedScene _selectedTower = null;
	private Tower _towerPreview = null;
	private Array<PackedScene> _towersToBuild = [];

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

		GenerateTowerSelectionButtons();
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed == true)
			{
				Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize);

				if (PathfindingManager.instance.TilemapBuildableData[mousePos] == true && IsInstanceValid(_towerPreview))
				{
					PathfindingManager.instance.TilemapBuildableData[mousePos] = false;

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
		_towerPreview = null;
	}

	public void SetSelectedTower(int index = -1)
	{
		_towerPreview?.QueueFree();

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
			TextureButton towerSelectionButton = TowerSelectionButtonScene.Instantiate<TextureButton>();
			int index = i;
			towerSelectionButton.Connect(BaseButton.SignalName.Pressed, Callable.From(() => SetSelectedTower(index)));

			// Change button textures to saved tower icon
			Texture2D towerIcon = ResourceLoader.Load<Texture2D>(_towersToBuild[i].ResourcePath[.._towersToBuild[i].ResourcePath.LastIndexOf('.')] + "Icon.png");
			towerSelectionButton.TextureNormal = towerIcon;
			towerSelectionButton.TexturePressed = towerIcon;

			TowerSelectionButtonContainer.AddChild(towerSelectionButton);
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
}
