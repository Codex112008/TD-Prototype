using Godot;
using System;
using System.Linq;

public partial class ReanimatorTower : Tower
{
    [Export] private Marker2D _firePoint;
    [Export] private Node2D _graveParent;
    [Export] private PackedScene _graveScene;

    private Timer _fireTimer;
    private CharacterBody2D _target = null;

    public override void _Ready()
    {
        base._Ready();

        _fireTimer = new Timer
        {
            OneShot = true,
        };
        AddChild(_fireTimer);

        _fireTimer.WaitTime = 1f / GetFinalTowerStats()[TowerStat.FireRate];

        EnemyManager.instance.Connect(EnemyManager.SignalName.EnemyDied, Callable.From((Enemy enemy) => OnEnemyDeath(enemy)));
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Projectile != null && Projectile.Effects.Count > 0 && !IsBuildingPreview)
        {
            if (_fireTimer.WaitTime != 1f / GetFinalTowerStats()[TowerStat.FireRate])
            {
                _fireTimer.WaitTime = 1f / GetFinalTowerStats()[TowerStat.FireRate];
            }

            if (GetTree().GetNodeCountInGroup("Enemy") > 0 || !Projectile.RequireEnemy)
            {
                if (_graveParent.GetChildren().Any(child => child is Node2D node && VectorInRange(node.GlobalPosition)))
                    _target = _graveParent.GetChildren().First(child => child is Node2D node && VectorInRange(node.GlobalPosition)) as CharacterBody2D;

                if (IsInstanceValid(_target) && VectorInRange(_target.GlobalPosition))
                {
                    if (_fireTimer.IsStopped())
                    {
                        Fire();
                        _fireTimer.Start();

                        if (_target.GetParent() == this)
                            _target.QueueFree();
                    }

                    if (_target is Enemy)
                        _target = TowerTargetingData.GetTargetedEnemy(CurrentTargeting, this);
                }
                else
                    _target = TowerTargetingData.GetTargetedEnemy(CurrentTargeting, this);
            }
        }
    }

    protected override void Fire()
    {
        Vector2 dir = GetCenteredGlobalPosition().DirectionTo(_target.GlobalPosition + _target.Velocity * (GetCenteredGlobalPosition().DistanceTo(_target.GlobalPosition) / Projectile.ProjectileSpeed));
        _firePoint.Rotation = dir.Angle() + (Mathf.Pi / 2f);
        Projectile.InstantiateProjectile(this, _firePoint, _target.GlobalPosition);
    }

    private void OnEnemyDeath(Enemy enemy)
    {
        if (VectorInRange(enemy.GlobalPosition) && !GetTree().GetNodesInGroup("Enemy").Any(node => node is ReanimatorGraveBehaviour grave && grave.GlobalPosition == enemy.GlobalPosition))
        {
            ReanimatorGraveBehaviour grave = _graveScene.Instantiate<ReanimatorGraveBehaviour>();
            grave.SpawnedWave = -1;
            grave.ReanimatorTower = this;
            grave.StoredEnemyStats = enemy.CurrentEnemyStats;
            grave.StoredEnemyTexture = enemy.Sprite.Texture;
            grave.TargetPos = enemy.TargetPos;
            grave.Position = _graveParent.ToLocal(enemy.GlobalPosition);

            _graveParent.CallDeferred(Node.MethodName.AddChild, grave);
        }
    }

    protected override int GetPointCostFromDamage()
    {
        return base.GetPointCostFromDamage() / 2;
    }

    protected override int GetPointCostFromFireRate()
    {
        return Mathf.FloorToInt((1f + (Mathf.Pow(TowerLevel, 1.6f) / 3f)) * (150f * BaseTowerStats[TowerStat.FireRate]));
    }

    protected override int GetPointCostFromRange()
    {
        return Mathf.FloorToInt(base.GetPointCostFromRange() * 1.5f);
    }
}
