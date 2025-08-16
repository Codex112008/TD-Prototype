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

		TowerCreatorController.instance.UpdateTowerPreview();
	}

	public void UpdateModifierSelector()
	{
		foreach (string modifierName in TowerCreatorController.GetFolderNames(PathToModifiers))
		{
			ModifierList.AddItem(TowerCreatorController.SplitIntoPascalCase(modifierName));
			// TODO: Also grab a sprite from each folder and set as icon in the future
		}
	}

	public void UpdatePathToSelectedModifierResource(int index)
	{
		bool isProjectile = PathToModifiers.Contains("Projectile");
		string subfolder = "Effects";
		if (isProjectile)
			subfolder = "Projectiles";

		string selectedModifier = TowerCreatorController.RemoveWhitespaces(ModifierList.GetItemText(index));
		PathToSelectedModifierResource = "res://Custom Resources/" + subfolder + "/" + selectedModifier + "/" + selectedModifier + subfolder[..^1] + "Resource.tres";
	}
}
