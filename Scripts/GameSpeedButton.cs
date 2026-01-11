using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class GameSpeedButton : TextureButton
{
	[Export] private Dictionary<Texture2D, int> _gameSpeedIcons;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Engine.TimeScale = _gameSpeedIcons[TextureNormal];
	}

	public void OnPressed()
	{
		TextureNormal = _gameSpeedIcons.Keys.ToArray()[(new Array<Texture2D>( _gameSpeedIcons.Keys.ToArray()).IndexOf(TextureNormal) + 1) % _gameSpeedIcons.Keys.Count];
		Engine.TimeScale = _gameSpeedIcons[TextureNormal];
		Engine.PhysicsTicksPerSecond = 60 * (int)Engine.TimeScale;
	}
}
