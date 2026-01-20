using Godot;
using System;

[GlobalClass]
public partial class StatusPulseEffect : EnemyEffect
{
    [Export] private PackedScene _pulseScene = GD.Load<PackedScene>("res://Custom Resources/EnemyEffects/StatusPulse/StatusPulseScene.tscn");
    [Export] private StatusEffect _status;
    [Export] private float _stacks;
    [Export] private float _range;
    [Export] private float _pauseLength;
    [Export] private Color _color;

    public override void ApplyEffect(Enemy enemy)
    {
        enemy.AddStatusEffectStacks(StatusEffect.Stun, _pauseLength * 10);

        StatusPulseBehaviour statusPulse = _pulseScene.Instantiate<StatusPulseBehaviour>();
        statusPulse.PulseCollider.Scale = Vector2.One * _range * PathfindingManager.instance.LevelTilemap.TileSet.TileSize.X / 40f;
        statusPulse.PulseSprite.SelfModulate = _color;
        statusPulse.Status = _status;
        statusPulse.Stacks = _stacks;
        statusPulse.Range = _range;
        EnemyManager.instance.EnemyParent.CallDeferred(Node.MethodName.AddChild, statusPulse);
        statusPulse.Position = enemy.GlobalPosition;
    }
}
