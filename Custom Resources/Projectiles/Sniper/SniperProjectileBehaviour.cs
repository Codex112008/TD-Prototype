using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class SniperProjectileBehaviour : RayCast2D
{
	[Export] private Array<RayCast2D> _raycasts = [];
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public SniperProjectile SniperData;

	private Tween _tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_raycasts.Insert(0, this);

		Line2D line = GetChild<Line2D>(0);

		Vector2 endPos = TargetPosition.Normalized() * (Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f);
		foreach (RayCast2D raycast in _raycasts)
		{
			raycast.ForceRaycastUpdate();
			if (raycast.IsColliding())
			{
				endPos = raycast.ToLocal(raycast.GetCollisionPoint());

				foreach (TowerEffect effect in SniperData.Effects)
					effect.ApplyEffect(Stats, (Enemy)raycast.GetCollider());

				break;
			}
		}
		line.SetPointPosition(1, endPos);
		
		_tween ??= CreateTween();
		_tween.TweenProperty(line, "width", 0, 0.2f).SetDelay(0.1f);
		_tween.SetParallel();
		_tween.TweenProperty(this, "modulate", Colors.Transparent, 0.25f).SetDelay(0.1f);
		_tween.TweenCallback(Callable.From(QueueFree)).SetDelay(0.5f);
	}
}
