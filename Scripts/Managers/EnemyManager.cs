using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EnemyManager : Node
{
	public static EnemyManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one EnemyManager in scene!");
			return;
		}
		instance = this;
	}

	[Export] private float _chanceForPartnerWave = 0.75f;
	[Export] private int _waveForSegmentScaling = 10;
	[Export] private Marker2D _spawnPoint;
	[Export] private Marker2D _enemyTarget;
	[Export] private Timer _waveTimer;
	[Export] private Timer _spawnTimer;
	[Export] public Node EnemyParent;
	[Export] public Array<EnemySpawnData> EnemiesToSpawnData = [];

	private List<Tuple<EnemySpawnData, float>> _enemySpawnQueue = []; // Should only be one wave at a time
	private int _currentWave = 0;
	private int _enemiesToSpawn;
	private int _tileSize;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_tileSize = PathfindingManager.instance.TileSize;

		foreach (EnemySpawnData spawnData in EnemiesToSpawnData)
			spawnData.Weight = spawnData.BaseWeight;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if ((_waveTimer.TimeLeft <= 0 || EnemyParent.GetChildCount() == 0) && _enemySpawnQueue.Count == 0)
		{
			_enemySpawnQueue = GenerateDynamicWave(EnemiesToSpawnData);
			_spawnTimer.WaitTime = _enemySpawnQueue[0].Item2;
			_spawnTimer.Start();
		}

		if (_enemySpawnQueue.Count > 0)
		{
			if (_spawnTimer.TimeLeft <= 0)
			{
				// Orders the wave based on 'difficulty' (maybe add a dedicated spawnorder/difficulty value to spawn data)
				_enemySpawnQueue = [.. _enemySpawnQueue.OrderBy(spawnData => spawnData.Item1.MinWave)];

				EnemySpawnData enemyToSpawn = _enemySpawnQueue[0].Item1;
				if (_enemySpawnQueue.Count > 1)
					_spawnTimer.WaitTime = _enemySpawnQueue[1].Item2;

				// Remove the enemyToSpawn obtained from wave and if the wave is fully spawned start timer for next wave
					_enemySpawnQueue.RemoveAt(0);
				if (_enemySpawnQueue.Count == 0)
					_waveTimer.Start();

				Enemy spawnedEnemy = enemyToSpawn.EnemyScene.Instantiate<Enemy>();
				spawnedEnemy.targetPos = _enemyTarget.GlobalPosition;
				spawnedEnemy.GlobalPosition = _spawnPoint.GlobalPosition;

				EnemyParent.AddChild(spawnedEnemy);

				_spawnTimer.Start();
			}
		}
	}

	public List<Tuple<EnemySpawnData, float>> GenerateDynamicWave(Array<EnemySpawnData> enemyPoolDatas)
	{
		RandomNumberGenerator rand = new();

		_currentWave++;

		List<Tuple<EnemySpawnData, float>> generatedWave = [];

		// Calculates the amount of enemy segments
		int enemySegments = 1 + _currentWave / _waveForSegmentScaling;

		// Segment scaling so increases in segments dont cause massive jumps in total enemy counts (log for diminishing returns like 1-2 and 3-4)
		float segmentScaling = 1.0f + Mathf.Log(1 + (enemySegments - 1 + _currentWave % _waveForSegmentScaling / (float)_waveForSegmentScaling) * 0.4f);
		for (int i = 0; i < enemySegments; i++)
		{
			float spawnDelay = 0.6f;
			bool condensedWave = rand.Randf() > 0.5f;
			if (condensedWave)
				spawnDelay = 0.3f;

			// Select random enemy based on weight and calculate the amount to spawn
			EnemySpawnData selectedEnemy = WeightedEnemyChoice(enemyPoolDatas);

			// Triangular distribution taken from nova drift
			float triangular = rand.Randf() - rand.Randf();
			float enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyHigh, triangular);
			if (triangular < 0)
				enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyLow, -triangular);

			// Wave scaling also taken from nova drift xd
			float waveScaling = 1.25f + 0.00416667f * (Mathf.Min(_currentWave - selectedEnemy.MinWave, 240) - 120);
			enemyCount *= waveScaling;

			// Segment scaling divided by segment count bc thats how math works
			enemyCount *= segmentScaling / enemySegments;

			// Partner wave generation
			float partnerWaveMultiplier = 0.65f;
			if (selectedEnemy.PairingChoices.Count > 0 && rand.Randf() < _chanceForPartnerWave && enemyPoolDatas == EnemiesToSpawnData)
			{
				enemyCount *= partnerWaveMultiplier;
				generatedWave.AddRange(GenerateDynamicWave(selectedEnemy.PairingChoices));
			}
			else if (enemyPoolDatas != EnemiesToSpawnData)
				enemyCount *= 1f - partnerWaveMultiplier;

			// Floor enemy count to get integer values
			int enemyCountInt = Mathf.Clamp(Mathf.FloorToInt(enemyCount), selectedEnemy.QtyMin, selectedEnemy.QtyMax);

			// Add enemies to wave
			generatedWave.InsertRange(0, [.. Enumerable.Range(0, enemyCountInt).Select(i => new Tuple<EnemySpawnData, float>(selectedEnemy, spawnDelay))]);
		}

		return generatedWave;
	}

	public EnemySpawnData WeightedEnemyChoice(Array<EnemySpawnData> enemiesToSpawnData)
	{
		Array<EnemySpawnData> enemySpawnData = [.. enemiesToSpawnData.Where(spawnData => _currentWave >= spawnData.MinWave && _currentWave <= spawnData.MaxWave)];

		RandomNumberGenerator rand = new();
		int totalWeight = enemySpawnData.Sum(spawnData => spawnData.Weight);
		int randomChoice = rand.RandiRange(1, totalWeight);
		int currentSum = 0;
		foreach (EnemySpawnData spawnData in enemySpawnData)
		{
			currentSum += spawnData.Weight;
			if (currentSum >= randomChoice)
				return spawnData;
		}
		return null;
	}
}
