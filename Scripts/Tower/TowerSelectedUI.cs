using Godot;
using System;

public partial class TowerSelectedUI : VBoxContainer
{
	[Export] public Button UpgradeButton;
	[Export] public Button SellButton;
	[Export] private RichTextLabel _targetingLabel;
	
	[Export] private Control _upgradeUI;

	private Tower _tower;
	private Timer _upgradeButtonTimer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_upgradeUI.Visible = false;
		_tower = GetParent<Tower>();
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

	public void ResetUpgradeButtonText()
	{	
		Tuple<string, int> towerPathAndLevel = Utils.TrimNumbersFromString(_tower.SceneFilePath[.._tower.SceneFilePath.LastIndexOf('.')]);
		Tower upgradedTower = ResourceLoader.Load<PackedScene>(towerPathAndLevel.Item1 + (towerPathAndLevel.Item2 + 1) + ".tscn", "PackedScene", ResourceLoader.CacheMode.Replace).Instantiate<Tower>();
        UpgradeButton.Text = "Upgrade: $" + Mathf.FloorToInt(upgradedTower.GetFinalTowerStats()[TowerStat.Cost]);
        upgradedTower.QueueFree();
	}

	public void ChangeTowerTargetingLeft()
	{
		_tower.CurrentTargeting = _tower.AllowedTargetingModes[Mathf.PosMod(_tower.AllowedTargetingModes.IndexOf(_tower.CurrentTargeting) - 1, _tower.AllowedTargetingModes.Count)];
		_targetingLabel.Text = _tower.CurrentTargeting.ToString();
	}

	public void ChangeTowerTargetingRight()
	{
		_tower.CurrentTargeting = _tower.AllowedTargetingModes[Mathf.PosMod(_tower.AllowedTargetingModes.IndexOf(_tower.CurrentTargeting) + 1, _tower.AllowedTargetingModes.Count)];
		_targetingLabel.Text = _tower.CurrentTargeting.ToString();
	}
}
