using Godot;
using Godot.Collections;
using System;

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
	private PackedScene _selectedTower = null;
	private Tower towerPreview = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < TowersToBuild.Count; i++)
		{
			TextureButton towerSelectionButton = TowerSelectionButtonScene.Instantiate<TextureButton>();
			towerSelectionButton.Pressed += () => SetSelectedTower(i);
			TowerSelectionButtonContainer.AddChild(towerSelectionButton);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_selectedTower != null)
		{
			MakePreviewFollowMouse();
		}
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseEvent)
			if (mouseEvent.ButtonIndex == MouseButton.Left && mouseEvent.Pressed == true)
				GD.Print("Left mouse button clicked");
	}

	public void MakePreviewFollowMouse()
	{
		Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / 64) * 64;
		towerPreview = _selectedTower.Instantiate<Tower>();
		towerPreview.IsBuildingPreview = true;
		towerPreview.Position = mousePos;
	}

	private void BuildTower()
	{

	}

	public void SetSelectedTower(int index = -1)
	{
		if (index != -1)
		{
			_selectedTower = TowersToBuild[index];
		}
		else
		{
			_selectedTower = null;
		}
	}
}
