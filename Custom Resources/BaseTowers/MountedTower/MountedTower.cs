using Godot;
using System;

public partial class MountedTower : Tower
{
	private Timer _fireTimer;

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
	public override void _PhysicsProcess(double delta)
	{
		base._PhysicsProcess(delta);

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
        throw new NotImplementedException();
    }
}
