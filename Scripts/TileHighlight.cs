using Godot;
using System;

public partial class TileHighlight : Sprite2D
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / 64);
		Position = mousePos * 64;
	}
}
