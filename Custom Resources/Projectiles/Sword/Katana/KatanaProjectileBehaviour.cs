using Godot;
using Godot.Collections;
using System;

public partial class KatanaProjectileBehaviour : CharacterBody2D
{
	[Export] private AnimationPlayer _animationPlayer;
	
	public Dictionary<TowerStat, float> Stats; // Has every stat but mostly damage being used
	public KatanaProjectile KatanaData;

	private Dictionary<TowerStat, float> _originalStats;
	private float _waveLifetime;
	private float _realAlpha;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_originalStats = new(Stats);

		_animationPlayer.Play("Swing");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _PhysicsProcess(double delta)
	{
		if (Modulate.A == 0)
			QueueFree();

		MoveAndSlide();
	}

	public void OnAnimFinished(StringName animName)
	{
		Velocity = -Transform.Y.Normalized() * KatanaData.WaveInitialSpeed;

		VisibleOnScreenNotifier2D notifier = new();
		AddChild(notifier);
		notifier.ScreenExited += OnScreenExited;

		_waveLifetime = Tower.ConvertTowerRangeToTiles(Stats[TowerStat.Range]) / KatanaData.WaveMaxSpeed * 2f;
		Timer timer = new()
		{
			WaitTime = _waveLifetime,
			Autostart = true,
			OneShot = true
		};
		timer.Connect(Timer.SignalName.Timeout, Callable.From(QueueFree));
		AddChild(timer);

		Tween tween = CreateTween();
		tween.TweenProperty(this, "velocity", -Transform.Y.Normalized() * KatanaData.WaveMaxSpeed, 0.5f).SetTrans(Tween.TransitionType.Expo);
	}

	public void OnBodyEntered(Node2D body)
	{
		if (body.IsInGroup("Enemy"))
		{
			foreach (TowerEffect effect in KatanaData.Effects)
				effect.ApplyEffect(Stats, (Enemy)body);

			Tween tween = CreateTween();
			tween.TweenProperty(this, "modulate", new Color(Modulate, Mathf.Max(0f, _realAlpha - (1 / KatanaData.Pierce))), 0.1f);
			_realAlpha -= 1 / KatanaData.Pierce;

			foreach (TowerStat stat in Stats.Keys)
				Stats[stat] -= _originalStats[stat] * (1 / KatanaData.Pierce);
		}
	}

	private void OnScreenExited()
    {
		QueueFree();
    }
}
