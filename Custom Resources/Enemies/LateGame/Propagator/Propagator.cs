using Godot;
using System;

public partial class Propagator : Enemy
{
	[Export] private EnemySpawnData _broodmotherSpawnData;
	[Export] private EnemySpawnData _carrierSpawnData;
	[Export] private float _broodmotherDamageThreshold = 400;
	private Timer _spawnTimer;
	private float _damageCounter = 0f;

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

		_currentStatusEffectDecayTimers[StatusEffect.Regen].QueueFree();
		_currentStatusEffectDecayTimers.Remove(StatusEffect.Regen);
    }

    public override float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
    {
		float damage = base.TakeDamage(amount, damageType, defenceBreak);
		if (!float.IsNaN(damage) && damage > 0)
			_damageCounter += damage;

		// Spawns carriers and broodmothers based on hp loss
		if (_spawnTimer.TimeLeft <= 0f && _damageCounter > 0)
			SpawnSummons();

        return damage;
    }

    protected override void UpdateStats()
    {
        base.UpdateStats();

		if (GetCurrentEnemyStatusEffectStacks(StatusEffect.Regen) > 0)
			CurrentEnemyStats[EnemyStat.Speed] *= 0.6f;
    }

    public override void AddStatusEffectStacks(StatusEffect status, float statusStacks, bool decay = false)
    {
        base.AddStatusEffectStacks(status, statusStacks, decay);

		if (status == StatusEffect.Regen)
			_currentStatusEffects[status] = Mathf.Min(2.67f, _currentStatusEffects[status]);
    }

	private async void SpawnSummons()
	{
		int broodmotherCount = Mathf.FloorToInt(_damageCounter / _broodmotherDamageThreshold);
		_damageCounter -= broodmotherCount * _broodmotherDamageThreshold;
		int carrierCount = Mathf.FloorToInt(_damageCounter / 25f);
		_damageCounter = 0;

		AddStatusEffectStacks(StatusEffect.Stun, (0.5f + (broodmotherCount * 0.1f) + (carrierCount * 0.05f)) * 40f);

		Timer timer = new()
		{
			WaitTime = 0.5f,
			Autostart = true,
			OneShot = true
		};
        AddChild(timer);

		await ToSignal(timer, Timer.SignalName.Timeout);

		RandomNumberGenerator rand = new();
		for (int i = 0; i < broodmotherCount; i++)
		{
			Enemy spawnedEnemy = _broodmotherSpawnData.EnemyScene.Instantiate<Enemy>();
			spawnedEnemy.TargetPos = EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)];
			spawnedEnemy.GlobalPosition = GlobalPosition;
			spawnedEnemy.SpawnedWave = SpawnedWave;

			EnemyManager.instance.EnemyParent.AddChild(spawnedEnemy);

			timer.Start();
			await ToSignal(timer, Timer.SignalName.Timeout);
		}

		timer.WaitTime = 0.05f;

		for (int i = 0; i < carrierCount; i++)
		{
			Enemy spawnedEnemy = _carrierSpawnData.EnemyScene.Instantiate<Enemy>();
			spawnedEnemy.TargetPos = EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)];
			spawnedEnemy.GlobalPosition = GlobalPosition;
			spawnedEnemy.SpawnedWave = SpawnedWave;

			EnemyManager.instance.EnemyParent.AddChild(spawnedEnemy);

			timer.Start();
			await ToSignal(timer, Timer.SignalName.Timeout);
		}

		timer.QueueFree();
		_spawnTimer.Start();
	}
}
