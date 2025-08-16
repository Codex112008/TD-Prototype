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

	[Export] public TileMapLayer LevelTileMap;

	private AStarGrid2D _aStarGrid = new();

	private const string MOVEMENT_COST = "MovementCost";

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetUpAStarGrid();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SetUpAStarGrid()
	{
		_aStarGrid.Region = LevelTileMap.GetUsedRect();
		_aStarGrid.CellSize = LevelTileMap.TileSet.TileSize;
		_aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles; // Maybe change this to smth else if dont like

		_aStarGrid.Update();

		UpdateTerrainMovementValues();
	}

	private void UpdateTerrainMovementValues()
	{
		foreach (Vector2I cellPos in LevelTileMap.GetUsedCells())
		{
			int tileMovementCost = (int)LevelTileMap.GetCellTileData(cellPos).GetCustomData(MOVEMENT_COST);
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

		RandomNumberGenerator rand = new();
		float offsetMargin = _aStarGrid.CellSize.X - 20f;
		Vector2 offset = new(rand.RandfRange(-offsetMargin / 2, offsetMargin / 2), rand.RandfRange(-offsetMargin / 2, offsetMargin / 2));

		foreach (Vector2I point in _aStarGrid.GetPointPath(startPos, endPos).Select(v => (Vector2I)v))
		{
			Vector2 currentPoint = point;
			currentPoint += (Vector2I)(_aStarGrid.CellSize / 2) + offset;

			pathArray.Add(currentPoint);
		}

		pathArray[0] -= offset;
		pathArray[^1] -= offset;

		return pathArray;
	}
}
