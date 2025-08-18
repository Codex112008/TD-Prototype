using Godot;
using System;

public partial class DamageNumber : Node2D
{
	[Export] private Curve _positionCurve;
	[Export] private RichTextLabel _numberLabel;
	public float DamageValue = 0;
	public DamageType DamageTypeDealt = DamageType.Physical;

	private Tween tween;
	private Color transparentColor;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_numberLabel.Position -= _numberLabel.Size / 2;
		_numberLabel.Text = DamageValue.ToString();

		Color color = DamageTypeColor.GetDamageTypeColor(DamageTypeDealt);
		_numberLabel.Set("theme_override_colors/default_color", color);
		transparentColor = new Color(color.R, color.G, color.B, 0);

		tween ??= CreateTween();
		tween.TweenMethod(
            Callable.From((float progress) =>
            {
				float curveProgress = _positionCurve.Sample(progress);
				Position = Position.Lerp(Position + Vector2.Down * 10, curveProgress);
			}),
			0.0, 1.0, 1.5f
		);
		tween.SetParallel();
		tween.TweenProperty(this, "scale", Vector2.One, 1f);
		tween.TweenProperty(this, "modulate", Colors.Transparent, 0.5f).SetDelay(0.25f);
		tween.TweenCallback(Callable.From(QueueFree)).SetDelay(1f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
