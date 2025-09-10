using Godot;
using Godot.Collections;
using System;

public partial class AOEEffect : TowerEffect
{
    [Export] private PackedScene _aoeScene;
    public override void ApplyEffect(Dictionary<TowerStat, float> stats, Enemy target)
    {
        AOEEffectBehaviour aoeScene = _aoeScene.Instantiate<AOEEffectBehaviour>();
        aoeScene.AOECollider.Scale = Vector2.One * stats[TowerStat.Range] * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 400f;
        aoeScene.GlobalPosition = target.GlobalPosition;
        aoeScene.Stats = stats;
        BuildingManager.instance.InstancedNodesParent.AddChild(aoeScene);
    }
}
