using Godot;
using Godot.Collections;
using System.Linq;

[GlobalClass]
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

	[Export] private int _waveForSegmentScaling = 10;
	[Export] private Marker2D _spawnPoint;
	[Export] private Marker2D _enemyTarget;
	[Export] private Timer _waveTimer;
	[Export] private Timer _spawnTimer;
	[Export] public Node EnemyParent;
	[Export] public Array<EnemySpawnData> EnemiesToSpawnData = [];

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
		if (_waveTimer.TimeLeft <= 0 || EnemyParent.GetChildCount() == 0)
		{
			GenerateDynamicWave();
			_waveTimer.Start();
		}
	}

	public void GenerateDynamicWave()
	{
		RandomNumberGenerator rand = new();
		
		_currentWave++;

		// Calculates the amount of enemy segments
		int enemySegments = 1 + _currentWave / _waveForSegmentScaling;

		// Segment scaling so increases in segments dont cause massive jumps in total enemy counts (log for diminishing returns)
		float segmentScaling = 1.0f + Mathf.Log(1 + (enemySegments - 1 + _currentWave % _waveForSegmentScaling / (float)_waveForSegmentScaling) * 0.4f);
		for (int i = 0; i < enemySegments; i++)
		{
			// Select random enemy based on weight and calculate the amount to spawn
			EnemySpawnData selectedEnemy = WeightedEnemyChoice(EnemiesToSpawnData);

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

			// Floor enemy count to get integer values
			int enemyCountInt = Mathf.Clamp(Mathf.FloorToInt(enemyCount), selectedEnemy.QtyMin, selectedEnemy.QtyMax);
		}
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

		//GD.PrintErr("Something went wrong, this really shold never happen");
		return null;
	}
}
