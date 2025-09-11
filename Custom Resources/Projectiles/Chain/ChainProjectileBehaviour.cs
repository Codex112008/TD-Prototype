using Godot;
using Godot.Collections;
using System;

public partial class ChainProjectileBehaviour : RayCast2D
{
	[Export] private int _chainCount = 2;
	[Export] private float _lineVariation = 5; // Smaller values for straighter line
	[Export] private int _pointCount = 5;
	[Export] private Array<RayCast2D> _raycasts = [];
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public ChainProjectile ChainData;
	public Array<Enemy> ChainedEnemies = [];

	private Tween _tween;
	private Line2D _line;
	private Vector2 _hitEnemyPosition;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_raycasts.Insert(0, this);
		_line = GetChild<Line2D>(0);

		Vector2 endPos = TargetPosition.Normalized() * (Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f);
		foreach (RayCast2D raycast in _raycasts)
		{
			raycast.ForceRaycastUpdate();
			if (raycast.IsColliding())
			{
				endPos = raycast.ToLocal(raycast.GetCollisionPoint());

				Enemy hitEnemy = (Enemy)raycast.GetCollider();
				foreach (TowerEffect effect in ChainData.Effects)
					effect.ApplyEffect(Stats, hitEnemy);
				ChainedEnemies.Add(hitEnemy);
				_hitEnemyPosition = hitEnemy.GlobalPosition;

				break;
			}
		}
		if (endPos != TargetPosition.Normalized() * (Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f) || ChainedEnemies.Count == 0)
			GenerateLine(endPos);

		_tween ??= CreateTween();
		_tween.TweenProperty(_line, "width", 0, 0.2f).SetDelay(0.1f);
		_tween.SetParallel();
		_tween.TweenProperty(this, "modulate", Colors.Transparent, 0.25f).SetDelay(0.1f);
		_tween.TweenCallback(Callable.From(QueueFree)).SetDelay(0.5f);
	}

	private async void GenerateLine(Vector2 endPos)
	{
		RandomNumberGenerator rand = new();

		float distanceToEndPos = Vector2.Zero.DistanceTo(endPos);
		Vector2 directionToEndPos = Vector2.Zero.DirectionTo(endPos);
		for (int i = 1; i < _pointCount; i++)
		{
			if (i != _pointCount - 1)
			{
				float pointDistance = distanceToEndPos * ((float)i / _pointCount);
				Vector2 pointPosition = (directionToEndPos * pointDistance) + new Vector2(rand.RandfRange(-_lineVariation, _lineVariation), rand.RandfRange(-_lineVariation, _lineVariation));
				_line.AddPoint(pointPosition);

				Timer timer = new() { WaitTime = 0.01f };
				AddChild(timer);
				timer.Start();
				await ToSignal(timer, Timer.SignalName.Timeout);
				timer.QueueFree();
			}
			else
				_line.AddPoint(endPos);
		}

		if (ChainedEnemies.Count <= _chainCount && endPos != TargetPosition.Normalized() * (Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f))
		{
			Enemy nextEnemy = FindClosestEnemyToTarget(endPos);
			Dictionary<TowerStat, float> halvedStats = [];
			foreach ((TowerStat stat, float value) in Stats)
				halvedStats[stat] = value / 2f;
			if (halvedStats[TowerStat.Damage] > 0.1f)
				ChainData.InstantiateProjectile(halvedStats, _hitEnemyPosition, _hitEnemyPosition.DirectionTo(nextEnemy.GlobalPosition).Angle() + Mathf.Pi / 2f, ChainedEnemies);
		}
	}

	private Enemy FindClosestEnemyToTarget(Vector2 targetPos)
    {
        Enemy closestEnemy = null;
        foreach (Node node in GetTree().GetNodesInGroup("Enemy"))
        {
            if (node is Enemy enemy)
            {
				float distanceToEnemy = ToGlobal(targetPos).DistanceTo(enemy.GlobalPosition);
                if ((closestEnemy == null || distanceToEnemy < ToGlobal(targetPos).DistanceTo(closestEnemy.GlobalPosition)) && !ChainedEnemies.Contains(enemy))
                {
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }
}
