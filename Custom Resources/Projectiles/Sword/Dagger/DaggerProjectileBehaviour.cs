using Godot;
using Godot.Collections;
using System;

public partial class DaggerProjectileBehaviour : RayCast2D
{
	private static int swingCount = 0;

	[Export] private Line2D _daggerTrail;
	[Export] private Area2D _daggerArea;
	[Export] private AnimationPlayer _animationPlayer;
	[Export] private Array<RayCast2D> _raycasts = [];
	[Export] private CharacterBody2D _body;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public DaggerProjectile DaggerData;

	private bool _melee = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (swingCount == 1)
			_body.Scale = new (-1.5f, 1.5f);

		_raycasts.Insert(0, this);

		Vector2 endPos = TargetPosition.Normalized() * (Stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 10f);
		foreach (RayCast2D raycast in _raycasts)
		{
			raycast.ForceRaycastUpdate();
			if (raycast.IsColliding())
			{
				endPos = raycast.ToLocal(raycast.GetCollisionPoint());

				if (Vector2.Zero.DistanceTo(endPos) < 20f)
					_melee = true;

				break;
			}
		}

		if (_melee)
			_animationPlayer.Play("Swing" + swingCount);
		else
		{
			foreach (TowerStat stat in Stats.Keys)
				Stats[stat] /= 2f;

			_body.Velocity = -Transform.Y.Normalized() * DaggerData.FireForce;

			VisibleOnScreenNotifier2D notifier = new();
			_body.AddChild(notifier);
			notifier.ScreenExited += OnScreenExited;
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (_daggerTrail.Visible)
		{
			Vector2 swordRelativeUpDirection = -_daggerArea.Transform.Y.Normalized();
			Vector2 swordRelativeRightDirection = _daggerArea.Transform.X.Normalized();
			Vector2 newPointVectorPos = swordRelativeRightDirection * 1f + swordRelativeUpDirection * 5f;
			_daggerTrail.AddPoint(newPointVectorPos);
		}

		if (_daggerTrail.Points.Length > 100)
			_daggerTrail.RemovePoint(0);

		_body.MoveAndSlide();
	}

	public void OnAnimFinished(StringName animName)
	{
		swingCount = (swingCount + 1) % 3;
		if (animName.ToString().Contains("Swing"))
			QueueFree();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			foreach (TowerEffect effect in DaggerData.Effects)
				effect.ApplyEffect(Stats, (Enemy)body);
			
			if (!_melee)
				QueueFree();
		}
	}

	private void OnScreenExited()
    {
		QueueFree();
    }
}
