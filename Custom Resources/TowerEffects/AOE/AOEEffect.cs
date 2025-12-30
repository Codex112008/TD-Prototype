using Godot;
using Godot.Collections;
using System;

public partial class AOEEffect : TowerEffect
{
    [Export] private PackedScene _aoeScene;
    protected override void ApplyEffectCore(Dictionary<TowerStat, float> stats, Enemy target)
    {
        AOEEffectBehaviour aoeScene = _aoeScene.Instantiate<AOEEffectBehaviour>();
        aoeScene.AOECollider.Scale = Vector2.One * _finalStats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 400f;
        aoeScene.GlobalPosition = target.GlobalPosition;
        aoeScene.Stats = _finalStats;
        BuildingManager.instance.InstancedNodesParent.CallDeferred("add_child", aoeScene);
    }
}
