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
    private CharacterBody2D _target = null;
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

            if (GetTree().GetNodeCountInGroup("Enemy") > 0 || !Projectile.RequireEnemy)
            {
                if (IsInstanceValid(_target) && VectorInRange(_target.GlobalPosition))
                {
                    Vector2 dir = GetCenteredGlobalPosition().DirectionTo(_target.GlobalPosition + _target.Velocity * (GetCenteredGlobalPosition().DistanceTo(_target.GlobalPosition) / Projectile.ProjectileSpeed));
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
        foreach (Marker2D firePoint in _firePoints)
        {
            firePoint.RotationDegrees = _rand.RandfRange(-_shotSpread, _shotSpread);
            
            Projectile.InstantiateProjectile(this, firePoint, Utils.RotateVectorAroundPoint(_target.GlobalPosition, GlobalPosition, firePoint.Rotation));
        }
    }

    protected override int GetPointCostFromDamage()
    {
        return Mathf.FloorToInt(5f * Mathf.Pow(1.8f, BaseTowerStats[TowerStat.Damage] + 4.66f));;
    }

    protected override int GetPointCostFromRange()
    {
        return base.GetPointCostFromRange() * 2;
    }
    
    protected override int GetPointCostFromFireRate()
    {
        return base.GetPointCostFromFireRate() * 2;
    }
}
