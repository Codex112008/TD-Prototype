using Godot;
using Godot.Collections;
using System.Linq;

public static class TowerTargetingData
{
	private static readonly Dictionary<TowerTargeting, Callable> _findEnemyWithTargetingFunctions = new()
	{
		{TowerTargeting.First, Callable.From((Tower tower) =>
			{
				Enemy firstEnemy = null;
				foreach (Node node in tower.GetTree().GetNodesInGroup("Enemy"))
				{
					if (node is Enemy enemy)
					{
						if (((firstEnemy == null) || enemy.PathArray.Count < firstEnemy.PathArray.Count && enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro) >= firstEnemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro)) && tower.VectorInRange(enemy.GlobalPosition))
						{
							firstEnemy = enemy;
						}
					}
				}
				return firstEnemy;
			})
		},
		{TowerTargeting.Last, Callable.From((Tower tower) =>
			{
				Enemy lastEnemy = null;
				foreach (Node node in tower.GetTree().GetNodesInGroup("Enemy"))
				{
					if (node is Enemy enemy)
					{
						if (((lastEnemy == null) || enemy.PathArray.Count > lastEnemy.PathArray.Count && enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro) >= lastEnemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro)) && tower.VectorInRange(enemy.GlobalPosition))
						{
							lastEnemy = enemy;
						}
					}
				}
				return lastEnemy;
			})
		},
		{TowerTargeting.Random, Callable.From((Tower tower) =>
			{
				Array<Enemy> enemiesInRange = [.. tower.GetTree().GetNodesInGroup("Enemy").Where(node => node is Enemy enemy && tower.VectorInRange(enemy.GlobalPosition)).ToArray().Cast<Enemy>()];
				if (enemiesInRange.Count > 0)
					return enemiesInRange.PickRandom();
				else
					return null;
			})
		},
		{TowerTargeting.Strong, Callable.From((Tower tower) =>
			{
				Enemy strongestEnemy = null;
				foreach (Node node in tower.GetTree().GetNodesInGroup("Enemy"))
				{
					if (node is Enemy enemy)
					{
						if (((strongestEnemy == null) || enemy.GetCurrentHealth() > strongestEnemy.GetCurrentHealth() && enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro) >= strongestEnemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro)) && tower.VectorInRange(enemy.GlobalPosition))
						{
							strongestEnemy = enemy;
						}
					}
				}
				return strongestEnemy;
			})
		},
		{TowerTargeting.Weak, Callable.From((Tower tower) =>
			{
				Enemy weakestEnemy = null;
				foreach (Node node in tower.GetTree().GetNodesInGroup("Enemy"))
				{
					if (node is Enemy enemy)
					{
						if (((weakestEnemy == null) || enemy.GetCurrentHealth() < weakestEnemy.GetCurrentHealth() && enemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro) >= weakestEnemy.GetCurrentEnemyStatusEffectStacks(StatusEffect.Aggro)) && tower.VectorInRange(enemy.GlobalPosition))
						{
							weakestEnemy = enemy;
						}
					}
				}
				return weakestEnemy;
			})
		},
	};

	public static CharacterBody2D GetTargetedEnemy(TowerTargeting targeting, Tower tower)
	{
		foreach(Node child in tower.GetChildren())
        {
            if (child is CharacterBody2D)
                child.QueueFree();
        }

		CharacterBody2D foundEnemy = (Enemy)_findEnemyWithTargetingFunctions[targeting].Call(tower);

		if (foundEnemy == null && !tower.Projectile.RequireEnemy)
		{
			foundEnemy = CreateDummyTarget(tower);
		}

		return foundEnemy;
	}

	public static CharacterBody2D CreateDummyTarget(Tower tower)
	{
		Array<Vector2I> walkableTilesInRange = tower.GetWalkableTilesInRange();
        if (walkableTilesInRange.Count > 0)
        {
            RandomNumberGenerator rand = new();
            Vector2 randomPos;
            do
                randomPos = PathfindingManager.instance.GetTileToGlobalPos(walkableTilesInRange[rand.RandiRange(0, walkableTilesInRange.Count - 1)]) + new Vector2(rand.RandfRange(6f, 10f), rand.RandfRange(6f, 10f));
            while(PathfindingManager.instance.IsTileAtGlobalPosSolid(randomPos));

            CharacterBody2D dummyBody = new();
            tower.AddChild(dummyBody);
            dummyBody.AddChild(new Sprite2D(){Texture = tower.Projectile.Icon});
            dummyBody.GlobalPosition = randomPos;
            return dummyBody;
        }
		else
			return null;
	}
}
