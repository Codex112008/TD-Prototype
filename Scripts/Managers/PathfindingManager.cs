using Godot;
using Godot.Collections;
using System;
using System.Linq;

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

	[Export] private TileMapLayer _levelTileMap;

	private AStarGrid2D _aStarGrid = new();
	private Array<Vector2I> _pathArray = [];

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
		_aStarGrid.Region = _levelTileMap.GetUsedRect();
		_aStarGrid.CellSize = _levelTileMap.TileSet.TileSize;
		_aStarGrid.DiagonalMode = AStarGrid2D.DiagonalModeEnum.OnlyIfNoObstacles; // Maybe change this to smth else if dont like

		_aStarGrid.Update();

		UpdateTerrainMovementValues();
	}

	private void UpdateTerrainMovementValues()
	{
		_pathArray.Clear();
		foreach (Vector2I cellPos in _levelTileMap.GetUsedCells())
		{
			int tileMovementCost = (int)_levelTileMap.GetCellTileData(cellPos).GetCustomData(MOVEMENT_COST);
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

	public Array<Vector2I> GetValidPath(Vector2I startPos, Vector2I endPos)
	{
		_pathArray.Clear();

		foreach (Vector2I point in _aStarGrid.GetPointPath(startPos, endPos).Select(v => (Vector2I)v))
		{
			Vector2I currentPoint = point;
			currentPoint += (Vector2I)(_aStarGrid.CellSize / 2);

            _pathArray.Add(currentPoint);
		}

		return _pathArray;
	}
}
