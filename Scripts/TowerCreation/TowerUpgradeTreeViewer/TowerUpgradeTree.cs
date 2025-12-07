using Godot;
using System;

public partial class TowerUpgradeTree : Control
{
	public string TowerPathToDisplay;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		DirAccess dirAccess = DirAccess.Open(TowerPathToDisplay);

		int maximumLevel = dirAccess.GetFiles().Length / 2;
		for (int i = 0; i < maximumLevel; i++)
        {
            
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
