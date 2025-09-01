using Godot;
using Godot.Collections;
using System;

public partial class ShotgunTower : Tower
{
	[Export] private Array<Marker2D> _firePoints = [];
    [Export] private Marker2D _pivotPoint;
    [Export] private float _rotateSpeed;
    [Export] private float _shotSpread = 2f;

    private Timer _fireTimer;
    private Enemy _target = null;
    private RandomNumberGenerator _rand = new();

    public override void _Ready()
    {
        base._Ready();

        _fireTimer = new Timer
        {
            OneShot = true,
        };
        AddChild(_fireTimer);

        _fireTimer.WaitTime = 1f / GetFinalTowerStats()[TowerStat.FireRate];
        _shotSpread /= 2;
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

            if (GetTree().GetNodeCountInGroup("Enemy") > 0)
            {
                Enemy firstEnemy = FindFirstEnemy();
                if (_target != firstEnemy)
                {
                    _target = firstEnemy;
                }

                if (_target != null)
                {
                    Vector2 dir = GetCenteredGlobalPosition().DirectionTo(_target.GlobalPosition + _target.Velocity * (GetCenteredGlobalPosition().DistanceTo(_target.GlobalPosition) / 250f));
                    float targetAngle = dir.Angle() + Mathf.Pi / 2f;
                    _pivotPoint.Rotation = Mathf.LerpAngle(_pivotPoint.Rotation, targetAngle, _rotateSpeed * (float)delta);

                    if (_fireTimer.IsStopped() && Mathf.Abs(Mathf.Wrap(targetAngle - _pivotPoint.Rotation, -Mathf.Pi, Mathf.Pi)) <= Mathf.Pi / 16f)
                    {
                        _pivotPoint.Rotation = targetAngle;
                        Fire();
                        _fireTimer.Start();
                    }
                }
            }
        }
    }

    private Enemy FindFirstEnemy()
    {
        Enemy firstEnemy = null;
        foreach (Node node in GetTree().GetNodesInGroup("Enemy"))
        {
            if (node is Enemy enemy)
            {
                float distanceToEnemy = GetCenteredGlobalPosition().DistanceTo(enemy.GlobalPosition);
                if ((firstEnemy == null || enemy.PathArray.Count < firstEnemy.PathArray.Count) && distanceToEnemy <= GetRangeInTiles())
                {
                    firstEnemy = enemy;
                }
            }
        }

        return firstEnemy;
    }

    protected override void Fire()
    {
        foreach (Marker2D firePoint in _firePoints)
        {
            firePoint.RotationDegrees = _rand.RandfRange(-_shotSpread, _shotSpread);
            Projectile.InstantiateProjectile(GetFinalTowerStats(), firePoint);
        }
    }
}
