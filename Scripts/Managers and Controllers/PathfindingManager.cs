using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public partial class PathfindingManager : Node
{
	public static PathfindingManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one PathfindingManager in scene!");
			return;
		}
		instance = this;
	}

	public TileMapLayer LevelTilemap;
	public int TileSize;
	public Dictionary<Vector2I, bool> TilemapBuildableData = [];
	//public Dictionary<Vector2I, int> TilemapMovementCostData = [];

	private AStarGrid2D _aStarGrid = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void Init()
	{
		LevelTilemap = (TileMapLayer)GetTree().GetFirstNodeInGroup("Tilemap");
		TileSize = LevelTilemap.TileSet.TileSize.X;
		foreach (Vector2I tilePos in LevelTilemap.GetUsedCells())
		{
			TilemapBuildableData[tilePos] = (bool)LevelTilemap.GetCellTileData(tilePos).GetCustomData("Buildable");
		}

		SetUpAStarGrid();
	}

	private void SetUpAStarGrid()
	{
		_aStarGrid.Region = LevelTilemap.GetUsedRect();
		_aStarGrid.CellSize = LevelTilemap.TileSet.TileSize;
		_aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.Never; // Maybe change this to smth else if dont like

		_aStarGrid.Update();

		UpdateTerrainMovementValues();
	}

	private void UpdateTerrainMovementValues()
	{
		foreach (Vector2I cellPos in LevelTilemap.GetUsedCells())
		{
			int tileMovementCost = (int)LevelTilemap.GetCellTileData(cellPos).GetCustomData("MovementCost");
			if (tileMovementCost < 10)
			{
				_aStarGrid.SetPointWeightScale(cellPos, tileMovementCost);
			}
			else
			{
				_aStarGrid.SetPointSolid(cellPos, true);
			}
		}
	}

	public Array<Vector2> GetValidPath(Vector2I startPos, Vector2I endPos)
	{
		Array<Vector2> pathArray = [];

		foreach (Vector2I point in _aStarGrid.GetPointPath(startPos, endPos).Select(v => (Vector2I)v))
		{
			Vector2 currentPoint = point;
			currentPoint += (Vector2I)(_aStarGrid.CellSize / 2);

			pathArray.Add(currentPoint);
		}
		
		return pathArray;
	}
}
