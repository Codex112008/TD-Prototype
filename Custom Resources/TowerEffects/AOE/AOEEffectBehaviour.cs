using Godot;
using Godot.Collections;
using System;
using System.Threading.Tasks;

public partial class AOEEffectBehaviour : Area2D
{
	[Export] public CollisionShape2D AOECollider;
	[Export] private Sprite2D _aoeSprite;
	[Export] private TowerEffect _aoeEffect; // Usually damage

	public Dictionary<TowerStat, float> Stats;

	private Tween _tween;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        _ = Init();
	}

	private async Task Init()
	{
		await ToSignal(GetTree(), "physics_frame");
		await ToSignal(GetTree(), "physics_frame");
		
		foreach (Node2D body in GetOverlappingBodies())
		{
			if (body is Enemy enemy)
			{
				_aoeEffect.ApplyEffect(Stats, enemy);
			}
		}

		_tween ??= CreateTween();
		_tween.TweenProperty(_aoeSprite, "scale", Vector2.One * Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 400f, 0.05f);
		_tween.TweenProperty(_aoeSprite, "scale", Vector2.Zero, 0.075f).SetDelay(0.05f);
		_tween.TweenCallback(Callable.From(QueueFree));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
