using Godot;
using Godot.Collections;
using System;

public partial class SwordProjectileBehaviour : Node2D
{
	private static int swingCount = 0;

	[Export] private Line2D _swordTrail;
	[Export] private Area2D _swordArea;
	[Export] private AnimationPlayer _animationPlayer;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public SwordProjectile SwordData;

	private int _hitCount = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_animationPlayer.Play("Swing" + swingCount);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (_swordTrail.Visible)
		{
			Vector2 swordRelativeUpDirection = -_swordArea.Transform.Y.Normalized();
			Vector2 swordRelativeRightDirection = _swordArea.Transform.X.Normalized();
			Vector2 newPointVectorPos = swordRelativeRightDirection * 1f + swordRelativeUpDirection * 5f;
			_swordTrail.AddPoint(newPointVectorPos);
		}

		if (_swordTrail.Points.Length > 100)
			_swordTrail.RemovePoint(0);
	}

	public void OnAnimFinished(StringName animName)
	{
		swingCount = (swingCount + 1) % 2;
		if (animName.ToString().Contains("Swing"))
			QueueFree();
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy") && _hitCount < SwordData.Pierce)
		{
			foreach (TowerEffect effect in SwordData.Effects)
				effect.ApplyEffect(Stats, (Enemy)body);
			_hitCount++;
		}
	}
}
