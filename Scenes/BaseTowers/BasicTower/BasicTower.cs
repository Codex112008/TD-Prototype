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
                _fireTimer.Start();
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
        float distanceToClosestEnemy = float.PositiveInfinity;
        foreach (Node node in GetTree().GetNodesInGroup("Enemy"))
        {
            if (node is Enemy enemy)
            {
                float distanceToEnemy = GlobalPosition.DistanceTo(enemy.GlobalPosition);
                if ((closestEnemy == null || distanceToEnemy < distanceToClosestEnemy) && distanceToEnemy <= GetFinalTowerStats()[TowerStat.Range] * GetRangeInTiles())
                {
                    closestEnemy = enemy;
                    distanceToClosestEnemy = GlobalPosition.DistanceTo(closestEnemy.GlobalPosition);
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
