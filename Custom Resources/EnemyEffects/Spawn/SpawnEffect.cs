using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class SpawnEffect : EnemyEffect
{
    [Export] public Array<EnemySpawnData> enemiesToSpawn;
    [Export] public int enemiesToSpawnCount = 1;

    public override void ApplyEffect(Enemy enemy)
    {
        foreach (EnemySpawnData spawnData in enemiesToSpawn)
        {
            if (spawnData.Weight == -1)
                spawnData.Weight = spawnData.BaseWeight;
        }

        RandomNumberGenerator rand = new();
        for (int i = 0; i < enemiesToSpawnCount; i++)
        {
            Enemy spawnedEnemy = EnemyManager.instance.WeightedEnemyChoice(enemiesToSpawn).EnemyScene.Instantiate<Enemy>();
            spawnedEnemy.TargetPos = EnemyManager.instance.BaseLocations[rand.RandiRange(0, EnemyManager.instance.BaseLocations.Count - 1)];
            spawnedEnemy.GlobalPosition = enemy.GlobalPosition;
            EnemyManager.instance.EnemyParent.AddChild(spawnedEnemy);
        }
    }
}
