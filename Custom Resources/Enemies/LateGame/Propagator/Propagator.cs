using Godot;
using System;

public partial class Propagator : Enemy
{
	[Export] private EnemySpawnData _broodmotherSpawnData;
	[Export] private EnemySpawnData _carrierSpawnData;
	[Export] private float _broodmotherDamageThreshold = 400;
	[Export] private float _regenStacksToAdd = 5f;
	private Timer _spawnTimer;
	private float _damageCounter = 0f;
	private bool _spawning = false;
	private bool _appliedRegen = false;

    public override void _Ready()
    {
        base._Ready();

		_spawnTimer = new()
		{
			WaitTime = 3f,
			Autostart = true,
			OneShot = true
		};
		AddChild(_spawnTimer);
    }

    public override float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
    {
		float damage = base.TakeDamage(amount, damageType, defenceBreak);
		if (!float.IsNaN(damage) && damage > 0)
			_damageCounter += damage;

		// Spawns carriers and broodmothers based on hp loss
		if (!_spawning && _spawnTimer.TimeLeft <= 0f && _damageCounter > 0)
			SpawnSummons();

		if (GetCurrentHealth() < (CurrentEnemyStats[EnemyStat.MaxHealth] * 0.4f) && !_appliedRegen)
		{
			AddStatusEffectStacks(StatusEffect.Regen, _regenStacksToAdd);
			_appliedRegen = true;
		}

        return damage;
    }

    protected override void UpdateStats()
    {
        base.UpdateStats();

		if (GetCurrentEnemyStatusEffectStacks(StatusEffect.Regen) > 0)
			CurrentEnemyStats[EnemyStat.Speed] *= 0.6f;
    }

	private async void SpawnSummons()
	{
		_spawning = true;

		int broodmotherCount = Mathf.FloorToInt(_damageCounter / _broodmotherDamageThreshold);
		_damageCounter -= broodmotherCount * _broodmotherDamageThreshold;
		int carrierCount = Mathf.FloorToInt(_damageCounter / 25f);
		_damageCounter = 0;

		if (broodmotherCount > 0 || carrierCount > 0)
		{
			// AddStatusEffectStacks(StatusEffect.Stun, (0.5f + (broodmotherCount * 0.1f) + (carrierCount * 0.05f)) * 10f);

			Timer timer = new()
			{
				WaitTime = 0.15f,
				Autostart = true,
				OneShot = true
			};
			AddChild(timer);

			await ToSignal(timer, Timer.SignalName.Timeout);

			RandomNumberGenerator rand = new();
			for (int i = 0; i < broodmotherCount; i++)
			{
				EnemyManager.instance.SpawnEnemy(_broodmotherSpawnData, GlobalPosition, EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)], SpawnedWave, false);

				timer.Start();
				await ToSignal(timer, Timer.SignalName.Timeout);
			}

			timer.WaitTime = 0.1f;

			for (int i = 0; i < carrierCount; i++)
			{
				EnemyManager.instance.SpawnEnemy(_carrierSpawnData, GlobalPosition, EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)], SpawnedWave, false);

				timer.Start();
				await ToSignal(timer, Timer.SignalName.Timeout);
			}

			timer.QueueFree();
			_spawnTimer.Start();
		}

		_spawning = false;
	}
}
