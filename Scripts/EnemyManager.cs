using Godot;
using Godot.Collections;
using System;

public partial class EnemyManager : Node2D
{
	[Export] private PackedScene enemyScene;
	[Export] private Timer cd;
	private Array<Vector2> waypoints = new();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		for (int i = 0; i < GetChildCount(); i++)
		{
			waypoints.Add(GetChild<Marker2D>(i).Position);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (Input.IsKeyPressed(Key.E) && cd.TimeLeft <= 0)
		{
			SpawnEnemy();
			cd.Start();
		}
	}

	private void SpawnEnemy()
	{
		Enemy enemy = enemyScene.Instantiate<Enemy>();
		enemy.Position = GetChild<Marker2D>(0).Position;
		enemy.waypoints = waypoints;

		GetTree().Root.AddChild(enemy);
	}
}
