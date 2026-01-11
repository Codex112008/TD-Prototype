using System;
using System.Linq;
using Godot;
using Godot.Collections;

public partial class HeliTower : Tower
{
    [Export] private Marker2D _firePoint;
    [Export] private CharacterBody2D _heli;
    [Export] private float _heliSpeed;
    [Export] private float _heliRange;

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

            if (CurrentTargeting == TowerTargeting.Mouse && _heli.GlobalPosition.DistanceTo(GetGlobalMousePosition()) > 5f && (_heli.Position.Length() < GetRangeInTiles() || GlobalPosition.DistanceTo(GetGlobalMousePosition()) < GetRangeInTiles()))
            {
                Vector2 dir = _heli.GlobalPosition.DirectionTo(GetGlobalMousePosition());
                float targetAngle = dir.Angle() + Mathf.Pi / 2f;
                _heli.Rotation = Mathf.LerpAngle(_heli.Rotation, targetAngle, 20f * (float)delta);
                _heli.Velocity = dir.Normalized() * _heliSpeed;
            }
            else if (CurrentTargeting == TowerTargeting.Mouse)
                _heli.Velocity = _heli.Velocity.Lerp(Vector2.Zero, 5f * (float)delta);

            if (GetTree().GetNodeCountInGroup("Enemy") > 0 || !Projectile.RequireEnemy)
            {
                _target = GetTargetedEnemyInHeliRange();
                if (IsInstanceValid(_target))
                {
                    if (CurrentTargeting == TowerTargeting.Persuit && _heli.GlobalPosition.DistanceTo(_target.GlobalPosition + _target.Velocity.Normalized() * 20f) > 2f && (_heli.Position.Length() < GetRangeInTiles() || GlobalPosition.DistanceTo(_target.GlobalPosition) < GetRangeInTiles()))
                    {
                        Vector2 targetDir = _heli.GlobalPosition.DirectionTo(_target.GlobalPosition + _target.Velocity.Normalized() * 20f);
                        _heli.Velocity = targetDir * _heliSpeed * 2f;
                        if (_heli.GlobalPosition.DistanceTo(_target.GlobalPosition) > _heliRange)
                        {
                            float targetAngle = targetDir.Angle() + Mathf.Pi / 2f;
                            _heli.Rotation = Mathf.LerpAngle(_heli.Rotation, targetAngle, 20f * (float)delta);
                        }
                    }
                    else if (CurrentTargeting == TowerTargeting.Persuit)
                        _heli.Velocity = _heli.Velocity.Lerp(Vector2.Zero, 5f * (float)delta);

                    if (_heli.GlobalPosition.DistanceTo(_target.GlobalPosition) < _heliRange)
                    {
                        Vector2 dirToEnemy = _heli.GlobalPosition.DirectionTo(_target.GlobalPosition + _target.Velocity * (_heli.GlobalPosition.DistanceTo(_target.GlobalPosition) / Projectile.ProjectileSpeed));
                        float targetAngle = dirToEnemy.Angle() + Mathf.Pi / 2f;
                        _heli.Rotation = Mathf.LerpAngle(_heli.Rotation, targetAngle, 20f * (float)delta);
                            
                        if (_fireTimer.IsStopped() && Mathf.Abs(Mathf.Wrap(targetAngle - _heli.Rotation, -Mathf.Pi, Mathf.Pi)) <= Mathf.Pi / 16f)
                        {
                            _heli.Rotation = targetAngle;
                            Fire();
                            _fireTimer.Start();

                            if (_target.GetParent() == this)
                                _target.QueueFree();
                        }
                    }
                }
                else if (CurrentTargeting == TowerTargeting.Persuit)
                    _heli.Velocity = _heli.Velocity.Lerp(Vector2.Zero, 5f * (float)delta);
            }
        }

        _heli.MoveAndSlide();
    }

    protected override void Fire()
    {
        Projectile.InstantiateProjectile(this, _firePoint, _target.GlobalPosition);
    }

    private CharacterBody2D GetTargetedEnemyInHeliRange()
    {
        if (CurrentTargeting == TowerTargeting.Mouse)
        {
            Array<CharacterBody2D> enemiesInHeliRange = [.. GetTree().GetNodesInGroup("Enemy").Where(node => node is CharacterBody2D enemy && enemy.GlobalPosition.DistanceTo(_heli.GlobalPosition) < _heliRange).Cast<CharacterBody2D>().OrderBy(enemy => enemy.GlobalPosition.DistanceTo(_heli.GlobalPosition)).ToArray()];
            if (enemiesInHeliRange.Count > 0)
                return enemiesInHeliRange[0];
            else if (!Projectile.RequireEnemy)
                return TowerTargetingData.CreateDummyTarget(this);
            else
                return null;
        }
        else
            return TowerTargetingData.GetTargetedEnemy(TowerTargeting.First, this);
    }
}
