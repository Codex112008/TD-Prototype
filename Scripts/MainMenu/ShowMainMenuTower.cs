using Godot;
using Godot.Collections;
using System;

public partial class ShowMainMenuTower : Node2D
{
	[Export] private Array<PackedScene> towers;
	private Timer _towerTimer;
	private Tower _towerPreview = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_towerTimer = new()
		{
			WaitTime = 2f,
			OneShot = false,
			Autostart = true
		};
		AddChild(_towerTimer);
		_towerTimer.Connect(Timer.SignalName.Timeout, Callable.From(SwapTower));
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private void SwapTower()
	{
		_towerPreview?.QueueFree();
		_towerPreview = towers.PickRandom().Instantiate<Tower>();
		_towerPreview.GlobalPosition = new Vector2I(14, 6) * PathfindingManager.instance.TileSize;
		_towerPreview.RangeAlwaysVisible = true;
		AddChild(_towerPreview);

		_towerTimer.WaitTime = 7.4f;
	}
}
