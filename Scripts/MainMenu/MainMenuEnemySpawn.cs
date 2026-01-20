using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class MainMenuEnemySpawn : Marker2D
{
	[Export] public Array<EnemySpawnData> EnemiesToSpawnData = [];
	[Export] private Node2D _enemyParent;
	
	private Array<Vector2> _baseLocations;
	private Array<Vector2> _spawnPoints;
	private Timer _spawnTimer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		PathfindingManager.instance.Init();
		_spawnPoints = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Spawn")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];
		_baseLocations = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Base")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];

		_spawnTimer = new()
		{
			WaitTime = 1f,
			OneShot = false,
			Autostart = true
		};
		AddChild(_spawnTimer);
		_spawnTimer.Connect(Timer.SignalName.Timeout, Callable.From(SpawnEnemy));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SpawnEnemy()
	{
		RandomNumberGenerator rand = new();
		Enemy spawnedEnemy = EnemiesToSpawnData.PickRandom().EnemyScene.Instantiate<Enemy>();
		spawnedEnemy.TargetPos = _baseLocations[rand.RandiRange(0, _baseLocations.Count - 1)];
		spawnedEnemy.GlobalPosition = _spawnPoints[rand.RandiRange(0, _spawnPoints.Count - 1)];
		spawnedEnemy.RegisterDeathSignal = false;

		_enemyParent.AddChild(spawnedEnemy);

		_spawnTimer.WaitTime = rand.RandfRange(1f, 1.5f);
	}
}
