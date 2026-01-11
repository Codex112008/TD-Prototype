using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class DomainTower : Tower
{
    [Export] private Marker2D _firePoint;
    [Export] private float _projectileSpawnRadius = 10f;

	private Timer _fireTimer;
	private RandomNumberGenerator _rand = new();
    private CharacterBody2D _target = null;

	// Called when the node enters the scene tree for the first time.
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
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
        for (int i = 0; i < TowerLevel + 1; i++)
        {
            Vector2 predictedTargetPos = _target.GlobalPosition + _target.Velocity * (_projectileSpawnRadius / Projectile.ProjectileSpeed);
            float randomAngle = _rand.RandfRange(0f, Mathf.Tau);
            _firePoint.GlobalPosition = predictedTargetPos + Vector2.One.Rotated(randomAngle).Normalized() * _projectileSpawnRadius;
            _firePoint.Rotation = _firePoint.GlobalPosition.DirectionTo(predictedTargetPos).Angle() + Mathf.Pi / 2f;
            Projectile.InstantiateProjectile(this, _firePoint, _target.GlobalPosition);
        }
    }

    protected override int GetPointCostFromDamage()
    {
        return Mathf.FloorToInt((1f + (Mathf.Pow(TowerLevel, 1.3f) / 3f)) * (100f * BaseTowerStats[TowerStat.Damage]));
    }
    
    protected override int GetPointCostFromFireRate()
    {
        return Mathf.FloorToInt((1f + (Mathf.Pow(TowerLevel, 1.1f) / 3f)) * (20f * BaseTowerStats[TowerStat.FireRate]));
        //return base.GetPointCostFromFireRate() / 3;
    }
}
