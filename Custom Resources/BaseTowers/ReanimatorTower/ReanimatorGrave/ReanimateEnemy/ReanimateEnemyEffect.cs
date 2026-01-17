using Godot;
using Godot.Collections;
using System;

public partial class ReanimateEnemyEffect : EnemyEffect
{
    [Export] private PackedScene _remnantScene;
    [Export] private Color _remnantTint;
    
    public override void ApplyEffect(Enemy enemy)
    {
        if (enemy is ReanimatorGraveBehaviour grave)
        {
            RandomNumberGenerator rand = new();
            Dictionary<TowerStat, float> remnantStats = grave.ReanimatorTower.GetFinalTowerStats();

            RemnantBehaviour remnant = _remnantScene.Instantiate<RemnantBehaviour>();

            remnant.MaxHealth = grave.StoredEnemyStats[EnemyStat.MaxHealth] * (float)(Math.Log10(remnantStats[TowerStat.Damage]) + 1f);
            remnant.Speed = grave.StoredEnemyStats[EnemyStat.Speed] * (float)(Math.Log2(remnantStats[TowerStat.FireRate]) + 1f);
            remnant.Tower = grave.ReanimatorTower;

            remnant.TargetPos = EnemyManager.instance.SpawnPoints[rand.RandiRange(0, EnemyManager.instance.SpawnPoints.Count - 1)];
            remnant.GlobalPosition = grave.GlobalPosition;

            remnant.Sprite.Texture = grave.StoredEnemyTexture;
            remnant.Modulate = _remnantTint;

            BuildingManager.instance.TowerParent.CallDeferred(Node.MethodName.AddChild, remnant);
        }
        else
            GD.PrintErr("Reanimate effect only works on graves!");
    }
}
