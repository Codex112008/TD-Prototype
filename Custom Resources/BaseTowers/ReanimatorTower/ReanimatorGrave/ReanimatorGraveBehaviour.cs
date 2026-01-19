using Godot;
using Godot.Collections;
using System;

public partial class ReanimatorGraveBehaviour : Enemy
{
	[Export] private CollisionShape2D collider;
	public ReanimatorTower ReanimatorTower;
	public Dictionary<EnemyStat, float> StoredEnemyStats = [];
	public Texture2D StoredEnemyTexture;

    public override void _Ready()
    {
        base._Ready();

		RegisterDeathSignal = false;
		GetTree().CreateTimer(1f).Connect(Timer.SignalName.Timeout, Callable.From(EnableGrave));
    }

	private void EnableGrave()
	{
		collider.Disabled = false;
		AddToGroup("Enemy");
	}
}
