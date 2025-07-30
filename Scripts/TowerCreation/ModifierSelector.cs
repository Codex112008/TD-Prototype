using Godot;
using Godot.Collections;
using System;

public partial class ModifierSelector : VBoxContainer
{
	[Export] public RichTextLabel ModifierLabel;
	[Export] public RichTextLabel CostLabel;
	[Export] private TextureButton _selectedModifierButton;
	[Export] public ItemList ModifierList;
	public string PathToSelectedModifierResource;
	public string PathToModifiers;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		ModifierList.Select(0);
		UpdatePathToSelectedModifierResource();
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

		UpdatePathToSelectedModifierResource();

		// TODO: Change _selectedModifierButton's texture to icon

		TowerCreatorController.instance.UpdateTowerPreview();
	}

	public void UpdateModifierSelector()
	{
		foreach (string modifierName in GetFolderNames(PathToModifiers))
		{
			ModifierList.AddItem(modifierName);
			// TODO: Also grab a sprite from each folder and set as icon in the future
		}
	}

	private Array<string> GetFolderNames(string path)
	{
		// Get all directories at the specified path
		string[] folders = DirAccess.GetDirectoriesAt(path);

		if (folders == null)
		{
			GD.PushError($"Failed to access directory: {path}");
			return [];
		}

		return [.. folders];
	}

	private void UpdatePathToSelectedModifierResource()
	{
		bool isProjectile = PathToModifiers.Contains("Projectile");
		string subfolder = "Effects";
		if (isProjectile)
			subfolder = "Projectiles";

		string selectedModifier = ModifierList.GetItemText(ModifierList.GetSelectedItems()[0]);
		PathToSelectedModifierResource = "res://Custom Resources/" + subfolder + "/" + selectedModifier + "/" + selectedModifier + subfolder[..^1] + "Resource.tres/";
	}
}
