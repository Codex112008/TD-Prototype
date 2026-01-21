using Godot;
using System;

public partial class TileHighlight : Sprite2D
{
	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (IsInstanceValid(PathfindingManager.instance))
		{
			Vector2I mousePos = PathfindingManager.instance.GetMouseTilemapPos();
			Position = Position.Lerp(mousePos * PathfindingManager.instance.TileSize, 30f * (float)(delta / Engine.TimeScale));

			TileData targetTile = PathfindingManager.instance.LevelTilemap.GetCellTileData(mousePos);
			if (targetTile == null || (PathfindingManager.instance.TilemapBuildableData[mousePos] == false && (int)targetTile.GetCustomData("MovementCost") > 10))
				SelfModulate = SelfModulate.Lerp(Colors.Transparent, 20f * (float)(delta / Engine.TimeScale));
			else
				SelfModulate = SelfModulate.Lerp(Colors.White, 20f * (float)(delta / Engine.TimeScale));
		}
	}
}
