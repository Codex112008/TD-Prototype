using Godot;
using Godot.Collections;
using System;

public partial class ReanimatorGraveBehaviour : Enemy
{
	public ReanimatorTower ReanimatorTower;
	public Dictionary<EnemyStat, float> StoredEnemyStats = [];
	public Texture2D StoredEnemyTexture;

    protected override void Die()
    {
        _isDead = true;

		TriggerEffects(EnemyEffectTrigger.OnDeath);

		QueueFree();
    }
}
