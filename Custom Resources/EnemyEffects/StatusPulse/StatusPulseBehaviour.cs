using Godot;
using Godot.Collections;
using System;

public partial class StatusPulseBehaviour : Area2D
{
	[Export] public Sprite2D PulseSprite;
	[Export] public StatusEffect Status;
	[Export] public float Stacks;
	[Export] public float Range;
	[Export] public CollisionShape2D PulseCollider;

	private Tween _tween;
	private bool applied = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_tween ??= CreateTween();
		_tween.TweenProperty(PulseSprite, "scale", Vector2.One * Range * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 40f, 0.5f);
		_tween.TweenProperty(PulseSprite, "scale", Vector2.Zero, 0.075f).SetDelay(0.05f);
		_tween.TweenCallback(Callable.From(QueueFree));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (!applied && GetOverlappingBodies().Count > 0)
		{
			foreach (Node2D body in GetOverlappingBodies())
			{
				if (body is Enemy enemy)
				{
					enemy.AddStatusEffectStacks(Status, Stacks);
				}
			}
			applied = true;
		}
	}
}
