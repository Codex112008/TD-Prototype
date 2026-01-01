using Godot;
using System;

public partial class QuitButton : Button
{
	public void OnPressed()
	{
		GetTree().Quit();
	}
}
