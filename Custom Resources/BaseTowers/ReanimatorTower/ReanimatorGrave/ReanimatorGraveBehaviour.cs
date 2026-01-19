using Godot;
using Godot.Collections;
using System;

public partial class ReanimatorGraveBehaviour : Enemy
{
	public ReanimatorTower ReanimatorTower;
	public Dictionary<EnemyStat, float> StoredEnemyStats = [];
	public Texture2D StoredEnemyTexture;

    public override void _Ready()
    {
        base._Ready();

		RegisterDeathSignal = false;
    }
}
