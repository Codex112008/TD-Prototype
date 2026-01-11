using Godot;
using Godot.Collections;
using System;
using System.Linq;

[GlobalClass]
public partial class SpawnEffect : EnemyEffect
{
    [Export] public Array<EnemySpawnData> _enemiesToSpawn;
    [Export] public int _enemiesToSpawnCount = 1;

    public override void ApplyEffect(Enemy enemy)
    {
        foreach (EnemySpawnData spawnData in _enemiesToSpawn)
        {
            if (spawnData.Weight == -1)
                spawnData.Weight = spawnData.BaseWeight;
        }

        RandomNumberGenerator rand = new();
        for (int i = 0; i < _enemiesToSpawnCount; i++)
        {
            Enemy spawnedEnemy = EnemyManager.instance.WeightedEnemyChoice(_enemiesToSpawn, false).EnemyScene.Instantiate<Enemy>();
            spawnedEnemy.TargetPos = EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)];
            spawnedEnemy.GlobalPosition = enemy.GlobalPosition;
            spawnedEnemy.SpawnedWave = enemy.SpawnedWave;

            EnemyManager.instance.EnemyParent.CallDeferred(Node.MethodName.AddChild, spawnedEnemy);
        }
    }
}
