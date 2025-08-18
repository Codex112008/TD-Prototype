using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class BuildingManager : Node2D
{
	public static BuildingManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one BuildingManager in scene!");
			return;
		}
		instance = this;
	}

	[Export] public Array<PackedScene> TowersToBuild = [];
	[Export] public PackedScene TowerSelectionButtonScene;
	[Export] public HBoxContainer TowerSelectionButtonContainer;
	[Export] private Node _towerParent;
	private PackedScene _selectedTower = null;
	private Tower _towerPreview = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GenerateTowerSelectionButtons();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_selectedTower != null && IsInstanceValid(_towerPreview))
		{
			MakePreviewFollowMouse();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton eventMouseButton)
		{
			if (eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed == true)
			{
				Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / 64);
				TileData cellTileData = PathfindingManager.instance.LevelTileMap.GetCellTileData(mousePos);

				if ((bool)cellTileData.GetCustomData("Buildable") == true && IsInstanceValid(_towerPreview))
				{
					cellTileData.SetCustomData("Buildable", false);

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

	public void MakePreviewFollowMouse()
	{
		Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize) * PathfindingManager.instance.TileSize;
		_towerPreview.Position = mousePos;
	}

	private void BuildTower()
	{
		_selectedTower = null;

		_towerPreview.IsBuildingPreview = false;
		_towerPreview = null;
	}

	public void SetSelectedTower(int index = -1)
	{
		if (index != -1)
		{
			if (TowersToBuild[index] != _selectedTower)
			{
				_selectedTower = TowersToBuild[index];

				_towerPreview = _selectedTower.Instantiate<Tower>();
				_towerPreview.IsBuildingPreview = true;
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
		
		for (int i = 0; i < TowersToBuild.Count; i++)
		{
			TextureButton towerSelectionButton = TowerSelectionButtonScene.Instantiate<TextureButton>();
			towerSelectionButton.Pressed += () => SetSelectedTower(i - 1);

			// Change button textures to saved tower icon
			Texture2D towerIcon = ImageTexture.CreateFromImage(Image.LoadFromFile(TowersToBuild[i].ResourcePath[..(TowersToBuild[i].ResourcePath.LastIndexOf('.') - 1)] + "Icon"));
			towerSelectionButton.TextureNormal = towerIcon;
			towerSelectionButton.TexturePressed = towerIcon;
			
			TowerSelectionButtonContainer.AddChild(towerSelectionButton);
		}
	}
}
