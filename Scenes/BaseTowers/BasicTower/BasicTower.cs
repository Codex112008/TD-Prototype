using Godot;
using System;

public partial class BasicTower : Tower
{
    [Export] private Marker2D _firePoint;
    [Export] private Marker2D _pivotPoint;
    [Export] private float _rotateSpeed;

    private Timer _fireTimer;
    private Enemy _target = null;

    public override void _Ready()
    {
        _fireTimer = new Timer
        {
            OneShot = true,
        };
        AddChild(_fireTimer);
    }

    public override void _Process(double delta)
    {
        if (Projectile != null && Projectile.Effects.Count > 0 && !IsBuildingPreview)
        {
            if (_fireTimer.WaitTime != 1f / GetFinalTowerStats()[TowerStat.FireRate])
            {
                _fireTimer.WaitTime = 1f / GetFinalTowerStats()[TowerStat.FireRate];
            }

            if (GetTree().GetNodeCountInGroup("Enemy") > 0)
            {
                if (_target == null)
                {
                    _target = FindClosestEnemy();
                }
                else
                {
                    Vector2 dir = GlobalPosition.DirectionTo(_target.GlobalPosition + _target.Velocity);
                    _pivotPoint.Rotation = Mathf.LerpAngle(_pivotPoint.Rotation, dir.Angle(), _rotateSpeed * (float)delta);

                    if (_fireTimer.TimeLeft <= 0)
                    {
                        Fire();
                        _fireTimer.Start();
                    }
                }
            }
        }
    }

    private Enemy FindClosestEnemy()
    {
        Enemy closestEnemy = null;
        foreach (Node node in GetTree().GetNodesInGroup("Enemy"))
        {
            if (node is Enemy enemy)
            {
                if ((closestEnemy == null || GlobalPosition.DistanceTo(enemy.GlobalPosition) < GlobalPosition.DistanceTo(closestEnemy.GlobalPosition)) && GlobalPosition.DistanceTo(enemy.GlobalPosition) <= GetFinalTowerStats()[TowerStat.Range])
                {
                    closestEnemy = enemy;
                }
            }
        }

        return closestEnemy;
    }

    protected override void Fire()
    {
        Projectile.InstantiateProjectile(GetFinalTowerStats(), _firePoint);
    }
}
