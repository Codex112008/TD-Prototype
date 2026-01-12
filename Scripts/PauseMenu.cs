using Godot;
using System;

public partial class PauseMenu : PanelContainer
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Engine.TimeScale = 1f;
		Visible = false;
	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			if (eventKey.Pressed && eventKey.Keycode == Key.Escape && !IsInstanceValid(BuildingManager.instance.TowerPreview))
			{
				if (Engine.TimeScale > 0)
					Pause();
				else
					UnPause();
			}
		}
	}

	private void Pause()
	{
		Engine.TimeScale = 0f;
		Visible = true;
	}

	private void UnPause()
	{
		Engine.TimeScale = 1f;
		Visible = false;
	}

	public void ReturnToMainMenu()
	{
		if (GetTree().GetNodesInGroup("Enemy").Count == 0)
			RunController.instance.SaveLevel();
		RunController.instance.DeloadRun();
		GetTree().ChangeSceneToFile(ProjectSettings.GlobalizePath("res://Scenes/MainScenes/MainMenu.tscn"));
	}
}
