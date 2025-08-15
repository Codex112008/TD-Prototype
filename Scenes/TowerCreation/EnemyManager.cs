using Godot;
using Godot.Collections;
using System;

public partial class EnemyManager : Node
{
	[Export] private Marker2D _spawnPoint;
	[Export] private Marker2D _enemyTarget;
	[Export] private PackedScene _enemyScene;
	//[Export] private Array<int> _waves;
	[Export] private Timer _waveTimer;
	[Export] private Timer _spawnTimer;

	private int _currentWave = -1;
	private int _enemiesToSpawn;
	private TileMapLayer _levelTileMap;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_levelTileMap = PathfindingManager.instance.LevelTileMap;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_waveTimer.TimeLeft <= 0)
		{
			_currentWave++;
			_enemiesToSpawn += 10;
			_waveTimer.Start();
		}

		if (_spawnTimer.TimeLeft <= 0 && _enemiesToSpawn > 0)
		{
			Enemy enemy = _enemyScene.Instantiate<Enemy>();
			_enemiesToSpawn--;

			enemy.GlobalPosition = (Vector2I)(_spawnPoint.Position / 64) * 64 + _levelTileMap.TileSet.TileSize / 2;
			enemy.targetPos = (Vector2I)(_enemyTarget.Position / 64) * 64 + _levelTileMap.TileSet.TileSize / 2;

			AddChild(enemy);

			_spawnTimer.Start();
		}
	}
}
