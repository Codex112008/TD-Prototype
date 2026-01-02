using Godot;
using System;

public partial class DomainTower : Tower
{
	[Export] private Marker2D _firePoint;

	private Timer _fireTimer;
	private RandomNumberGenerator _rand = new();

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

            if (_fireTimer.IsStopped())
            {
                Fire();
                _fireTimer.Start();
            }
        }
	}

    protected override void Fire()
    {
		// Randomises firepoint pos and rotation
		float distance;
		float angle;
		do
        {
            distance = _rand.RandfRange(0f, GetRangeInTiles());
            angle = _rand.RandfRange(0f, Mathf.Tau);
            _firePoint.Position = distance * Vector2.FromAngle(angle);
        } while (!PathfindingManager.instance.IsTileAtGlobalPosSolid(_firePoint.GlobalPosition) && !Projectile.RequireEnemy);
		_firePoint.Rotation = _rand.RandfRange(0f, Mathf.Tau);

		Projectile.InstantiateProjectile(this, _firePoint, _firePoint.GlobalPosition);
    }

	protected override int GetPointCostFromDamage()
    {
        return BaseTowerStats[TowerStat.Damage] * 50;
    }

	protected override int GetPointCostFromRange()
    {
        return BaseTowerStats[TowerStat.Range] * -1;
    }

	protected override int GetPointCostFromFireRate()
    {
        return BaseTowerStats[TowerStat.FireRate] * 50;
    }
}
