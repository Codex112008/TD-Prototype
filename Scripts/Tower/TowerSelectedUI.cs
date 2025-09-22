using Godot;
using System;

public partial class TowerSelectedUI : VBoxContainer
{
	[Export] private Control _upgradeUI;
	private Tower _tower;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_upgradeUI.Visible = false;
		_tower = GetParent<Tower>();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateUpgradeUIVisibility(bool visibility)
	{
		if (_upgradeUI.Visible != visibility)
			_upgradeUI.Visible = visibility;
	}

	public void OnUpgradeButtonClicked()
	{
		_tower.Upgrade();
	}

	public void OnSellButtonClicked()
	{
		_tower.Sell();
	}
}
