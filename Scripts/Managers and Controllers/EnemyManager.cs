using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EnemyManager : Node, IManager
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
	[Export] private Timer _spawnTimer;
	[Export] private HBoxContainer WaveButtonContainer;
	[Export] public Node EnemyParent;
	[Export] public Array<EnemySpawnData> EnemiesToSpawnData = [];

	public int CurrentWave = 0;
	public bool InLevel = false;
	public bool InTowerCreator = false;

	private List<Tuple<EnemySpawnData, float>> _enemySpawnQueue = []; // Should only be one wave at a time
	private int _tileSize;
	private Array<Vector2> _spawnPoints;
	private Array<Vector2> _baseLocations;
	private RichTextLabel _waveCounter;
	private Button _startWaveButton;
	private RandomNumberGenerator _tempRand = null; // Keep a temp copy of rand so can restore old version if saving in middle of wave for deterministic rng

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (EnemySpawnData spawnData in EnemiesToSpawnData)
			spawnData.Weight = spawnData.BaseWeight;

		_waveCounter = WaveButtonContainer.GetChild<RichTextLabel>(0);
		_startWaveButton = WaveButtonContainer.GetChild<Button>(1);
		_startWaveButton.Pressed += StartWave;

		RNGManager.instance.AddNewRNG(this);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (InLevel)
		{
			if (_enemySpawnQueue.Count > 0 && _startWaveButton.Disabled == false)
				_startWaveButton.Disabled = true;

			if (_enemySpawnQueue.Count > 0 && _spawnTimer.TimeLeft <= 0)
				SpawnQueuedEnemy();

			_waveCounter.Text = "Wave " + CurrentWave;
		}
		else if (InTowerCreator && _spawnTimer.TimeLeft <= 0)
		{
			Enemy spawnedEnemy = EnemiesToSpawnData[0].EnemyScene.Instantiate<Enemy>();
			spawnedEnemy.targetPos = _baseLocations[RNGManager.instance.RandInstances[this].RandiRange(0, _baseLocations.Count - 1)];
			spawnedEnemy.GlobalPosition = _spawnPoints[RNGManager.instance.RandInstances[this].RandiRange(0, _spawnPoints.Count - 1)];

			EnemyParent.AddChild(spawnedEnemy);

			_spawnTimer.Start();
		}
	}

	public void Init()
	{
		_tempRand = null;

		_tileSize = PathfindingManager.instance.TileSize;

		_enemySpawnQueue = [];

		_spawnPoints = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Spawn")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];
		_baseLocations = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Base")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];

		foreach (Node child in EnemyParent.GetChildren())
			child.QueueFree();

		InLevel = RunController.instance.CurrentScene.SceneFilePath == RunController.instance.LevelScene.ResourcePath;
		InTowerCreator = RunController.instance.CurrentScene.SceneFilePath == RunController.instance.TowerCreationScene.ResourcePath;

		if (InTowerCreator)
		{
			_startWaveButton.Disabled = true;
			_startWaveButton.Visible = false;

			_spawnTimer.WaitTime = 1f;
			_spawnTimer.Start();

			_waveCounter.Text = "Testing Towers";
		}
		else if (InLevel)
		{
			_startWaveButton.Disabled = false;
			_startWaveButton.Visible = true;
		}
	}

	private void StartWave()
	{
		CurrentWave++;

		if (_tempRand != null)
			RNGManager.instance.RandInstances[this] = _tempRand;

		_tempRand = new()
		{
			Seed = RNGManager.instance.RandInstances[this].Seed,
			State = RNGManager.instance.RandInstances[this].State
		};

		_enemySpawnQueue = GenerateDynamicWave(EnemiesToSpawnData);
		_spawnTimer.WaitTime = _enemySpawnQueue[0].Item2;
		_spawnTimer.Start();
	}

	private void SpawnQueuedEnemy()
	{
		// Orders the wave based on 'difficulty' (maybe add a dedicated spawnorder/difficulty value to spawn data)
		_enemySpawnQueue = [.. _enemySpawnQueue.OrderBy(spawnData => spawnData.Item1.MinWave)];

		EnemySpawnData enemyToSpawn = _enemySpawnQueue[0].Item1;
		if (_enemySpawnQueue.Count > 1)
			_spawnTimer.WaitTime = _enemySpawnQueue[1].Item2;

		// Remove the enemyToSpawn obtained from wave and if the wave is fully spawned start timer for next wave
		_enemySpawnQueue.RemoveAt(0);
		if (_enemySpawnQueue.Count == 0)
		{
			_startWaveButton.Disabled = false;
		}

		Enemy spawnedEnemy = enemyToSpawn.EnemyScene.Instantiate<Enemy>();
		spawnedEnemy.targetPos = _baseLocations[_tempRand.RandiRange(0, _baseLocations.Count - 1)];
		spawnedEnemy.GlobalPosition = _spawnPoints[_tempRand.RandiRange(0, _spawnPoints.Count - 1)];

		EnemyParent.AddChild(spawnedEnemy);

		_spawnTimer.Start();
	}

	public List<Tuple<EnemySpawnData, float>> GenerateDynamicWave(Array<EnemySpawnData> enemyPoolDatas)
	{
		List<Tuple<EnemySpawnData, float>> generatedWave = [];

		// Calculates the amount of enemy segments
		int enemySegments = 1 + CurrentWave / _waveForSegmentScaling;

		// Segment scaling so increases in segments dont cause massive jumps in total enemy counts (log for diminishing returns like 1-2 and 3-4)
		float segmentScaling = 1.0f + Mathf.Log(1 + (enemySegments - 1 + CurrentWave % _waveForSegmentScaling / (float)_waveForSegmentScaling) * 0.4f);
		for (int i = 0; i < enemySegments; i++)
		{
			float spawnDelay = 0.6f;
			bool condensedWave = _tempRand.Randf() > 0.5f;
			if (condensedWave)
				spawnDelay = 0.3f;

			// Select random enemy based on weight and calculate the amount to spawn
			EnemySpawnData selectedEnemy = WeightedEnemyChoice(enemyPoolDatas);

			// Triangular distribution taken from nova drift
			float triangular = _tempRand.Randf() - _tempRand.Randf();
			float enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyHigh, triangular);
			if (triangular < 0)
				enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyLow, -triangular);

			// Wave scaling also taken from nova drift xd
			float waveScaling = 1.25f + 0.00416667f * (Mathf.Min(CurrentWave - selectedEnemy.MinWave, 240) - 120);
			enemyCount *= waveScaling;

			// Segment scaling divided by segment count bc thats how math works
			enemyCount *= segmentScaling / enemySegments;

			// Partner wave generation
			float partnerWaveMultiplier = 0.65f;
			if (selectedEnemy.PairingChoices.Count > 0 && _tempRand.Randf() < _chanceForPartnerWave && enemyPoolDatas == EnemiesToSpawnData)
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
		Array<EnemySpawnData> enemySpawnData = [.. enemiesToSpawnData.Where(spawnData => CurrentWave >= spawnData.MinWave && CurrentWave <= spawnData.MaxWave)];

		int totalWeight = enemySpawnData.Sum(spawnData => spawnData.Weight);
		int randomChoice = _tempRand.RandiRange(1, totalWeight);
		int currentSum = 0;
		foreach (EnemySpawnData spawnData in enemySpawnData)
		{
			currentSum += spawnData.Weight;
			if (currentSum >= randomChoice)
				return spawnData;
		}
		return null;
	}

    public void Deload()
    {
		instance = null;
    }
}
