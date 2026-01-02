using Godot;

public partial class BasicTower : Tower
{
    [Export] private Marker2D _firePoint;
    [Export] private Marker2D _pivotPoint;
    [Export] private float _rotateSpeed;

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
                if (IsInstanceValid(_target) && VectorInRange(_target.GlobalPosition))
                {
                    Vector2 dir = GetCenteredGlobalPosition().DirectionTo(_target.GlobalPosition + _target.Velocity * (GetCenteredGlobalPosition().DistanceTo(_target.GlobalPosition) / 250f));
                    float targetAngle = dir.Angle() + Mathf.Pi / 2f;
                    _pivotPoint.Rotation = Mathf.LerpAngle(_pivotPoint.Rotation, targetAngle, _rotateSpeed * (float)delta);

                    if (_fireTimer.IsStopped() && Mathf.Abs(Mathf.Wrap(targetAngle - _pivotPoint.Rotation, -Mathf.Pi, Mathf.Pi)) <= Mathf.Pi / 16f)
                    {
                        _pivotPoint.Rotation = targetAngle;
                        Fire();
                        _fireTimer.Start();

                        if (_target.GetParent() == this)
                            _target.QueueFree();
                    }
                }
                else
                    _target = FindFirstEnemy();
            }
        }
    }

    protected override void Fire()
    {
        Projectile.InstantiateProjectile(this, _firePoint, _target.GlobalPosition);
    }
}
