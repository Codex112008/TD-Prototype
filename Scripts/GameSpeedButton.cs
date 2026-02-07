using System.Linq;
using Godot;
using Godot.Collections;

public partial class GameSpeedButton : TextureButton
{
	[Export] private Dictionary<Texture2D, int> _gameSpeedIcons;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Engine.TimeScale = _gameSpeedIcons[TextureNormal];
	}

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && keyEvent.Keycode == Key.Left)
			ChangeSpeedLeft();
    }

	public void OnPressed()
	{
		TextureNormal = _gameSpeedIcons.Keys.ToArray()[(new Array<Texture2D>(_gameSpeedIcons.Keys.ToArray()).IndexOf(TextureNormal) + 1) % _gameSpeedIcons.Keys.Count];
		Engine.TimeScale = _gameSpeedIcons[TextureNormal];
		Engine.PhysicsTicksPerSecond = 60 * (int)Engine.TimeScale;
	}

	public void ChangeSpeedLeft()
	{
		TextureNormal = _gameSpeedIcons.Keys.ToArray()[Mathf.PosMod(new Array<Texture2D>(_gameSpeedIcons.Keys.ToArray()).IndexOf(TextureNormal) - 1, _gameSpeedIcons.Keys.Count)];
		Engine.TimeScale = _gameSpeedIcons[TextureNormal];
		Engine.PhysicsTicksPerSecond = 60 * (int)Engine.TimeScale;
	}
}
