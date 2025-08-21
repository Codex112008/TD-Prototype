using Godot;
using Godot.Collections;
using System;

public partial class GameManager : Node
{
	public static GameManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one GameManager in scene!");
			return;
		}
		instance = this;
	}
}
