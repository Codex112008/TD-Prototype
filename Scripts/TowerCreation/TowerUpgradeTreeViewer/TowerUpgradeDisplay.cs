using Godot;
using System;
using System.Collections.Generic;

public partial class TowerUpgradeDisplay : PanelContainer
{
	public Tower TowerToDisplayUpgrade;
	public int TowerLevel;

	[Export] private PackedScene _towerComponentDisplay;
	[Export] private RichTextLabel _nameLabel;
	[Export] private VBoxContainer _statsContainer;
	[Export] private HBoxContainer _projectileContainer;
	[Export] private VBoxContainer _effectsContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
    {
		// Sets the name label's text
        _nameLabel.Text = TowerLevel switch
        {
            0 => Utils.SplitIntoPascalCase((string)TowerToDisplayUpgrade.Name),
            _ => "Level " + TowerLevel,
        };

		// Fills in the stats display of upgrade display
		foreach (KeyValuePair<TowerStat, int> stat in TowerToDisplayUpgrade.BaseTowerStats)
        {
            RichTextLabel statLabel = new()
            {
                Text = stat.Key.ToString() + ": " + stat.Value,
				FitContent = true,
				AutowrapMode = TextServer.AutowrapMode.Off
            };
			_statsContainer.AddChild(statLabel);
        }

		// Fills in the projectile of the upgrade display
		Projectile towerToDisplayUpgradeProjectile = TowerToDisplayUpgrade.Projectile;
		_projectileContainer.GetChild<RichTextLabel>(0).Text = "Projectile:";
		_projectileContainer.GetChild<TextureRect>(1).Texture = towerToDisplayUpgradeProjectile.Icon;
		_projectileContainer.GetChild<RichTextLabel>(2).Text = towerToDisplayUpgradeProjectile.ResourceName;

		// Fills in the effects of the upgrade display
		for (int i = 0; i < towerToDisplayUpgradeProjectile.Effects.Count; i++)
        {
			TowerEffect effect = towerToDisplayUpgradeProjectile.Effects[i];

            HBoxContainer towerEffectDisplay = _towerComponentDisplay.Instantiate<HBoxContainer>();
			towerEffectDisplay.GetChild<RichTextLabel>(0).Text = "Effect " + (i + 1) + ":";
			towerEffectDisplay.GetChild<TextureRect>(1).Texture = effect.Icon;
			towerEffectDisplay.GetChild<RichTextLabel>(2).Text = effect.ResourceName;

			_effectsContainer.AddChild(towerEffectDisplay);
        }
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton eventMouseButton && eventMouseButton.ButtonIndex == MouseButton.Left && eventMouseButton.Pressed)
		{
			TowerUpgradeTree.instance.ChangeTowerPreview(TowerLevel);
		}
    }
}
