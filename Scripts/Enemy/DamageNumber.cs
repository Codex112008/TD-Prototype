using Godot;
using System;

public partial class DamageNumber : Node2D
{
	[Export] private Curve _positionCurve;
	[Export] private RichTextLabel _numberLabel;
	public float DamageValue = 0;
	public DamageType DamageTypeDealt = DamageType.Physical;

	private Tween _tween;
	private float _offset = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		RandomNumberGenerator rand = new();
		_offset = rand.RandfRange(-35f, 35f);
		_numberLabel.Position -= _numberLabel.Size / 2;
		_numberLabel.Text = DamageValue.ToString();

		Color color = DamageTypeData.GetDamageTypeColor(DamageTypeDealt);
		_numberLabel.Set("theme_override_colors/default_color", color);

		_tween ??= CreateTween();
		_tween.TweenMethod(
			Callable.From((float progress) =>
			{
				float curveProgress = _positionCurve.Sample(progress);
				Position = Position.Lerp(Position + Vector2.Down * 2.5f, curveProgress);
			}),
			0.0, 1.0, 1.5f
		);
		_tween.SetParallel();
		_tween.TweenProperty(this, "position:x", Position.X + _offset, 1.5f);
		if (Scale != Vector2.One * 0.25f)
			_tween.TweenProperty(this, "scale", Vector2.One * 0.25f, 1f);
		_tween.TweenProperty(this, "modulate", Colors.Transparent, 0.25f).SetDelay(0.1f);
		_tween.TweenCallback(Callable.From(QueueFree)).SetDelay(0.5f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
