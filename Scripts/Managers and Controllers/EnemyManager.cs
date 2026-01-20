using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Linq;

public partial class EnemyManager : Node, IManager
{
	[Signal] public delegate void EnemyDiedEventHandler(Enemy enemy);

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
	[Export] private HBoxContainer _waveButtonContainer;
	[Export] private PackedScene _baseScene;
	[Export] public Array<int> TowerSlotUnlockWave;
	[Export] public Node EnemyParent;
	[Export] public Array<EnemySpawnData> EnemiesToSpawnData = [];

	public int CurrentWave = 0;
	public bool InLevel = false;
	public bool InTowerCreator = false;
	public Array<Vector2> BaseLocations;
	public Array<Vector2> SpawnPoints;
	public EnemySpawnData SelectedTestingEnemy;
	public Button StartWaveButton;

	private List<Tuple<EnemySpawnData, bool>> _enemySpawnQueue = []; // Should only be one wave at a time
	private int _tileSize;
	private RichTextLabel _waveCounter;
	private RandomNumberGenerator _tempRand = null; // Keep a temp copy of rand so can restore old version if saving in middle of wave for deterministic rng

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		foreach (EnemySpawnData spawnData in EnemiesToSpawnData)
			spawnData.Weight = spawnData.BaseWeight;

		_waveCounter = _waveButtonContainer.GetChild<RichTextLabel>(0);
		StartWaveButton = _waveButtonContainer.GetChild<Button>(1);
		StartWaveButton.Pressed += StartWave;

		RNGManager.instance.AddNewRNG(this);

		SelectedTestingEnemy = EnemiesToSpawnData[0];
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (InLevel)
		{
			if (_enemySpawnQueue.Count > 0 && StartWaveButton.Disabled == false)
				StartWaveButton.Disabled = true;

			if (_enemySpawnQueue.Count > 0 && _spawnTimer.IsStopped())
				SpawnQueuedEnemy();

			_waveCounter.Text = "Wave " + CurrentWave;
		}
		else if (InTowerCreator && _spawnTimer.IsStopped())
		{
			RandomNumberGenerator rand = new();
			Enemy spawnedEnemy = SelectedTestingEnemy.EnemyScene.Instantiate<Enemy>();
			spawnedEnemy.TargetPos = BaseLocations[rand.RandiRange(0, BaseLocations.Count - 1)];
			spawnedEnemy.GlobalPosition = SpawnPoints[rand.RandiRange(0, SpawnPoints.Count - 1)];

			EnemyParent.AddChild(spawnedEnemy);

			_spawnTimer.Start();
		}

		if (StartWaveButton.Disabled == false && Input.IsPhysicalKeyPressed(Key.Space))
		{
			StartWave();
		}
	}

	public void Init()
	{
		_tempRand = null;

		_tileSize = PathfindingManager.instance.TileSize;

		_enemySpawnQueue = [];

		if (IsInstanceValid(PathfindingManager.instance))
		{
			SpawnPoints = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Spawn")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];
			BaseLocations = [.. PathfindingManager.instance.LevelTilemap.GetUsedCells().Where(tilePos => (bool)PathfindingManager.instance.LevelTilemap.GetCellTileData(tilePos).GetCustomData("Base")).Select(PathfindingManager.instance.LevelTilemap.MapToLocal)];
			foreach (Vector2 location in BaseLocations)
			{
				Node2D baseInstance = _baseScene.Instantiate<Node2D>();
				baseInstance.GlobalPosition = location;
				PathfindingManager.instance.LevelTilemap.AddChild(baseInstance);
			}
		}

		foreach (Node child in EnemyParent.GetChildren())
			child.QueueFree();

		if (IsInstanceValid(RunController.instance))
		{
			InLevel = RunController.instance.CurrentScene.SceneFilePath == RunController.instance.LevelScene.ResourcePath;
			InTowerCreator = RunController.instance.CurrentScene.SceneFilePath == RunController.instance.TowerCreationScene.ResourcePath || RunController.instance.CurrentScene.SceneFilePath == RunController.instance.TowerUpgradeTreeViewerScene.ResourcePath;
		}

		if (InTowerCreator)
		{
			StartWaveButton.Disabled = true;
			StartWaveButton.Visible = false;

			_spawnTimer.WaitTime = 1f;
			_spawnTimer.Start();

			_waveCounter.Text = "Testing Towers";
		}
		else if (InLevel)
		{
			StartWaveButton.Disabled = false;
			StartWaveButton.Visible = true;
		}
	}

	private void StartWave()
	{
		RunController.instance.SaveLevel();
		
		CurrentWave++;

		if (TowerSlotUnlockWave.Any(wave => CurrentWave == wave))
			BuildingManager.instance.UpdateTowerSelectionButtons();

		if (_tempRand != null)
				RNGManager.instance.RandInstances[this] = _tempRand;

		_tempRand = new()
		{
			Seed = RNGManager.instance.RandInstances[this].Seed,
			State = RNGManager.instance.RandInstances[this].State
		};

		_enemySpawnQueue = GenerateDynamicWave(EnemiesToSpawnData);
		SpawnQueuedEnemy();
		_spawnTimer.WaitTime = _enemySpawnQueue[0].Item1.BaseSpawnDelay * (_enemySpawnQueue[0].Item2 ? 0.75f : 1f);
		_spawnTimer.Start();
	}

	private void SpawnQueuedEnemy()
	{
		// Orders the wave based on speed which is usually infered from spawn delay (maybe add a dedicated spawnorder/difficulty value to spawn data)
		_enemySpawnQueue = [.. _enemySpawnQueue.OrderByDescending(spawnData => spawnData.Item1.BaseSpawnDelay)];

		Tuple<EnemySpawnData, bool> enemyToSpawn = _enemySpawnQueue[0];

		// Remove the enemyToSpawn obtained from wave and if the wave is fully spawned start timer for next wave
		_enemySpawnQueue.RemoveAt(0);
		if (_enemySpawnQueue.Count == 0)
		{
			StartWaveButton.Disabled = false;
		}
		else if (_enemySpawnQueue[0].Item1.EnemyScene.ResourcePath != enemyToSpawn.Item1.EnemyScene.ResourcePath)
		{
			_spawnTimer.WaitTime += enemyToSpawn.Item1.BaseSpawnDelay;
		}
		_spawnTimer.WaitTime = enemyToSpawn.Item1.BaseSpawnDelay * (enemyToSpawn.Item2 ? 0.75f : 1f);

		Enemy spawnedEnemy = enemyToSpawn.Item1.EnemyScene.Instantiate<Enemy>();
		spawnedEnemy.TargetPos = BaseLocations[_tempRand.RandiRange(0, BaseLocations.Count - 1)];
		spawnedEnemy.GlobalPosition = SpawnPoints[_tempRand.RandiRange(0, SpawnPoints.Count - 1)];
		spawnedEnemy.SpawnedWave = CurrentWave;

		// Adds effect to give cash on death
		if (spawnedEnemy.Effects.TryGetValue(EnemyEffectTrigger.OnDeath, out Array<EnemyEffect> onDeathEffects))
			spawnedEnemy.Effects[EnemyEffectTrigger.OnDeath] = onDeathEffects + [new RewardEffect()];
		else
			spawnedEnemy.Effects.Add(EnemyEffectTrigger.OnDeath, [new RewardEffect()]);

		EnemyParent.AddChild(spawnedEnemy);

		_spawnTimer.Start();
	}

	public List<Tuple<EnemySpawnData, bool>> GenerateDynamicWave(Array<EnemySpawnData> enemyPoolDatas)
	{
		List<Tuple<EnemySpawnData, bool>> generatedWave = [];

		// Calculates the amount of enemy segments (each segment rolls a different enemy type, more segments for more types of enemies)
		int enemySegments = 1 + CurrentWave / _waveForSegmentScaling;

		// Segment scaling so increases in segments dont cause massive jumps in total enemy counts (log for diminishing returns like 1-2 and 3-4)
		float segmentScaling = 1.0f + Mathf.Log(1 + (enemySegments - 1 + CurrentWave % _waveForSegmentScaling / (float)_waveForSegmentScaling) * 0.4f);
		for (int i = 0; i < enemySegments; i++)
		{
			// Random chance for a wave with half spawn delay (enemies instantiated closer together)
			bool condensedWave = _tempRand.Randf() > 0.5f;

			// Select random enemy based on weights
			EnemySpawnData selectedEnemy = WeightedEnemyChoice(enemyPoolDatas);
			if (selectedEnemy == null)
				return [];

			// Distribue enemy count between the high, mean and low values
			float triangular = _tempRand.Randf() - _tempRand.Randf();
			float enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyHigh, triangular);
			if (triangular < 0)
				enemyCount = Mathf.Lerp(selectedEnemy.QtyMean, selectedEnemy.QtyLow, -triangular);

			// Scales enemy count based on wave number
			float waveScaling = 1.25f + 0.00416667f * (Mathf.Min(CurrentWave - selectedEnemy.MinWave, 240f) - 120);
			enemyCount *= waveScaling;

			// Apply segment scaling (divided by segment count)
			enemyCount *= segmentScaling / enemySegments;

			// Partner wave generation (chance to spawn a paired enemy type alongside the main one, and reduces main enemy count accordingly)
			float partnerWaveMultiplier = 0.65f;
			if (selectedEnemy.PairingChoices.Count > 0 && _tempRand.Randf() < _chanceForPartnerWave && enemyPoolDatas == EnemiesToSpawnData)
			{
				List<Tuple<EnemySpawnData, bool>> partnerEnemies = GenerateDynamicWave(selectedEnemy.PairingChoices);
				if (partnerEnemies.Count > 0f)
				{
					enemyCount *= partnerWaveMultiplier;
					generatedWave.AddRange(partnerEnemies);
				}
			}
			else if (enemyPoolDatas != EnemiesToSpawnData)
				enemyCount *= 1f - partnerWaveMultiplier;

			// Floor enemy count to get integer values
			int enemyCountInt = Mathf.Clamp(Mathf.FloorToInt(enemyCount), selectedEnemy.QtyMin, selectedEnemy.QtyMax);

			// Add enemies to wave
			generatedWave.AddRange([.. Enumerable.Range(0, enemyCountInt).Select(i => new Tuple<EnemySpawnData, bool>(selectedEnemy, condensedWave))]);
		}

		return generatedWave;
	}

	public EnemySpawnData WeightedEnemyChoice(Array<EnemySpawnData> enemiesToSpawnData, bool generatingWave = true)
	{
		RandomNumberGenerator rand  = new();
		if (_tempRand != null)
		{
			rand = new()
			{
				Seed = _tempRand.Seed,
				State = _tempRand.State
			};
		}

		Array<EnemySpawnData> enemySpawnData = [.. enemiesToSpawnData];
		if (generatingWave)
			enemySpawnData = [.. new Array<EnemySpawnData>(enemiesToSpawnData).Where(spawnData => CurrentWave >= spawnData.MinWave && CurrentWave <= spawnData.MaxWave)];

		int totalWeight = enemySpawnData.Sum(spawnData => spawnData.Weight);
		int randomChoice = rand.RandiRange(1, totalWeight);
		int currentSum = 0;
		foreach (EnemySpawnData spawnData in enemySpawnData)
		{
			currentSum += spawnData.Weight;
			if (currentSum >= randomChoice)
				return spawnData;
		}

		if (generatingWave)
		{
			_tempRand.Seed = rand.Seed;
			_tempRand.State = rand.State;
		}

		return null;
	}

	public void Deload()
	{
		instance = null;
	}
}