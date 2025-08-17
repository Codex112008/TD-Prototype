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
        base._Ready();

        _fireTimer = new Timer
        {
            OneShot = true,
        };
        AddChild(_fireTimer);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        if (Projectile != null && Projectile.Effects.Count > 0 && !IsBuildingPreview)
        {
            if (_fireTimer.WaitTime != 1f / GetFinalTowerStats()[TowerStat.FireRate])
            {
                _fireTimer.WaitTime = 1f / GetFinalTowerStats()[TowerStat.FireRate];
                _fireTimer.Start();
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
                    Vector2 dir = GetCenteredGlobalPosition().DirectionTo(_target.GlobalPosition + _target.Velocity * (GetCenteredGlobalPosition().DistanceTo(_target.GlobalPosition) / 1000));
                    _pivotPoint.Rotation = Mathf.LerpAngle(_pivotPoint.Rotation, dir.Angle() + Mathf.Pi / 2f, _rotateSpeed * (float)delta);

                    if (_fireTimer.TimeLeft <= 0 && Mathf.Abs(Mathf.Wrap(dir.Angle() + Mathf.Pi / 2f - _pivotPoint.Rotation, -Mathf.Pi, Mathf.Pi)) <= Mathf.Pi / 32f)
                    {
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
        Projectile.InstantiateProjectile(GetFinalTowerStats(), _firePoint);
    }
}
