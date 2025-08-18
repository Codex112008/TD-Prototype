using Godot;
using Godot.Collections;
using System;

public partial class ModifierSelector : VBoxContainer
{
	[Export] public RichTextLabel ModifierLabel;
	[Export] public RichTextLabel CostLabel;
	[Export] private TextureButton _selectedModifierButton;
	[Export] public ItemList ModifierList;
	[Export] public string PathToSelectedModifierResource;
	public string PathToModifiers;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnButtonPressed()
	{
		ModifierList.Visible = !ModifierList.Visible;
	}

	public void OnItemSelected(int index)
	{
		ModifierList.Visible = !ModifierList.Visible;

		UpdatePathToSelectedModifierResource(index);

		// TODO: Change _selectedModifierButton's texture to icon
		_selectedModifierButton.TextureNormal = ModifierList.GetItemIcon(index);
		_selectedModifierButton.TexturePressed = ModifierList.GetItemIcon(index);

		TowerCreatorController.instance.UpdateTowerPreview();
	}

	public void UpdateModifierSelector()
	{
		foreach (string modifierName in GameManager.GetFolderNames(PathToModifiers))
		{
			string componentResourcePath = PathToModifiers + modifierName + "/" + modifierName;
			if (PathToModifiers.Contains("Projectile"))
				componentResourcePath += "ProjectileResource.tres";
			else
				componentResourcePath += "EffectResource.tres";

			TowerComponent component = GD.Load<TowerComponent>(componentResourcePath);
			int index = ModifierList.AddItem(TowerCreatorController.SplitIntoPascalCase(modifierName), component.Icon);
			ModifierList.SetItemTooltip(index, component.Tooltip);
			ModifierList.SetItemTooltipEnabled(index, true);
		}
	}

	public void UpdatePathToSelectedModifierResource(int index)
	{
		bool isProjectile = PathToModifiers.Contains("Projectile");
		string selectedModifier = TowerCreatorController.RemoveWhitespaces(ModifierList.GetItemText(index));
		if (isProjectile)
			PathToSelectedModifierResource = "res://Custom Resources/Projectiles/" + selectedModifier + "/" + selectedModifier + "ProjectileResource.tres";
		else
			PathToSelectedModifierResource = "res://Custom Resources/Effects/" + selectedModifier + "/" + selectedModifier + "EffectResource.tres"; ;
	}
}
