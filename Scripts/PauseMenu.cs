using Godot;
using System;

public partial class PauseMenu : PanelContainer
{
	[Export] private Button _mainMenuButton;
	private double _timescaleBeforePause = 1;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Engine.TimeScale = 1f;
		Visible = false;

		_mainMenuButton.Connect(BaseButton.SignalName.Pressed, Callable.From(() => ReturnToMainMenu(this)));
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
		_timescaleBeforePause = Engine.TimeScale;
		Engine.TimeScale = 0f;
		Visible = true;
	}

	private void UnPause()
	{
		Engine.TimeScale = _timescaleBeforePause;
		Visible = false;
	}

	public static void ReturnToMainMenu(Node node)
	{
		Engine.TimeScale = 1f;
		if (node.GetTree().GetNodesInGroup("Enemy").Count == 0 && EnemyManager.instance.CurrentWave > 0)
			RunController.instance.SaveLevel();
		RunController.instance.DeloadRun();
		node.GetTree().ChangeSceneToFile(ProjectSettings.GlobalizePath("res://Scenes/MainScenes/MainMenu.tscn"));
	}
}
