using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public partial class SummonAlliesEffect : EnemyEffect
{
    [Export] public Array<EnemySpawnData> enemiesToSpawn;
    [Export] public int enemiesToSpawnCount;

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

            Vector2 centeredTileGlobalPos = PathfindingManager.instance.GlobalToCenteredGlobalTilePos(enemy.GlobalPosition);
            Vector2 centeredTileTargetPos = PathfindingManager.instance.GlobalToCenteredGlobalTilePos(enemy.PathArray[1]);
            float distanceToTargetTile = centeredTileGlobalPos.DistanceTo(centeredTileGlobalPos);
            float randomDistance = rand.RandfRange(0f, distanceToTargetTile);
            Vector2 directionToTargetTile = centeredTileGlobalPos.DirectionTo(centeredTileTargetPos).Normalized();
            Vector2 offsetVector = new Vector2(-directionToTargetTile.Y, directionToTargetTile.X).Normalized() * rand.RandfRange(-PathfindingManager.instance.TileSize / 2, PathfindingManager.instance.TileSize / 2);

            spawnedEnemy.GlobalPosition = (directionToTargetTile * randomDistance) + offsetVector;

            EnemyManager.instance.EnemyParent.AddChild(spawnedEnemy);
        }
    }
}
