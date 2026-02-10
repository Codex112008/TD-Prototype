using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public partial class SummonAlliesEffect : EnemyEffect
{
    [Export] private Array<EnemySpawnData> _enemiesToSpawn;
    [Export] private int _enemiesToSpawnCount;
    [Export] private float _spawnDelay = 0.1f;

    public async override void ApplyEffect(Enemy enemy)
    {
        enemy.AddStatusEffectStacks(StatusEffect.Stun, (1.5f + (_spawnDelay * (_enemiesToSpawnCount - 1))) * 10);

        foreach (EnemySpawnData spawnData in _enemiesToSpawn)
        {
            if (spawnData.Weight == -1)
                spawnData.Weight = spawnData.BaseWeight;
        }

        Timer timer = new()
		{
			WaitTime = 0.5f,
			Autostart = true,
			OneShot = true
		};
        enemy.AddChild(timer);

        await ToSignal(timer, Timer.SignalName.Timeout);

        timer.WaitTime = _spawnDelay;

        RandomNumberGenerator rand = new();
        for (int i = 0; i < _enemiesToSpawnCount; i++)
        {
            Vector2 centeredTileGlobalPos = PathfindingManager.instance.GlobalToCenteredGlobalTilePos(enemy.GlobalPosition);
            Vector2 centeredTileTargetPos = PathfindingManager.instance.GlobalToCenteredGlobalTilePos(-enemy.Transform.Y.Normalized() * 8f);
            if (enemy.PathArray.Count > 0)
                centeredTileTargetPos = PathfindingManager.instance.GlobalToCenteredGlobalTilePos(enemy.PathArray[1]);
            float distanceToTargetTile = Mathf.Min(centeredTileGlobalPos.DistanceTo(centeredTileTargetPos), 16f);
            float randomDistance = rand.RandfRange(Mathf.Min(8f, distanceToTargetTile), distanceToTargetTile);
            Vector2 directionToTargetTile = centeredTileGlobalPos.DirectionTo(centeredTileTargetPos).Normalized();
            Vector2 offsetVector = new Vector2(-directionToTargetTile.Y, directionToTargetTile.X).Normalized() * rand.RandfRange(-(PathfindingManager.instance.TileSize * 0.75f) / 2, PathfindingManager.instance.TileSize * 0.75f / 2);

            Enemy spawnedEnemy = EnemyManager.instance.SpawnEnemy(EnemyManager.instance.WeightedEnemyChoice(_enemiesToSpawn, false), centeredTileGlobalPos + (directionToTargetTile * randomDistance) + offsetVector, EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)], enemy.SpawnedWave, false);
            spawnedEnemy.GetChild<Sprite2D>(0).Rotation = directionToTargetTile.Angle();
                        
            if (i == _enemiesToSpawnCount - 1)
                timer.WaitTime = 1f;

            timer.Start();
            await ToSignal(timer, Timer.SignalName.Timeout);
        }

        timer.QueueFree();
    }
}
