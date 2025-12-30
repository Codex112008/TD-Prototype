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
		float distance = _rand.RandfRange(0f, GetRangeInTiles());
		float angle = _rand.RandfRange(0f, Mathf.Tau);
		_firePoint.Position = distance * Vector2.FromAngle(angle);
		_firePoint.Rotation = _rand.RandfRange(0f, Mathf.Tau);

		Projectile.InstantiateProjectile(GetFinalTowerStats(), _firePoint);
    }
}
