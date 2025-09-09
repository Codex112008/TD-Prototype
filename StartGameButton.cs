using Godot;
using System;

public partial class StartGameButton : Button
{
	[Export] public PackedScene levelScene;

	public void OnPressed()
	{
		GetTree().ChangeSceneToPacked(levelScene);
	}
}
