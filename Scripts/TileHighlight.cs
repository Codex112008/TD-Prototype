using Godot;
using System;

public partial class TileHighlight : Sprite2D
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		Vector2I mousePos = (Vector2I)(GetGlobalMousePosition() / PathfindingManager.instance.TileSize);
		Position = Position.Lerp(mousePos * PathfindingManager.instance.TileSize, 30 * (float)delta);

		TileData targetTile = PathfindingManager.instance.LevelTileMap.GetCellTileData(mousePos);
		if (targetTile == null || ((bool)targetTile.GetCustomData("Buildable") == false && (int)targetTile.GetCustomData("MovementCost") > 9))
			SelfModulate = SelfModulate.Lerp(Colors.Transparent, 20 * (float)delta);
		else
			SelfModulate = SelfModulate.Lerp(Colors.White, 20 * (float)delta);
	}
}
