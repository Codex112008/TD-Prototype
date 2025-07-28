using Godot;
using Godot.Collections;
using System;

public partial class ModifierSelector : VBoxContainer
{
	[Export] public RichTextLabel ModifierLabel;
	[Export] private TextureButton _selectedModifierButton;
	[Export] public ItemList ModifierList;
	public string PathToSelectedModifierResource;
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

		bool isProjectile = PathToModifiers.Contains("Projectile");
		string subfolder = "Effects";
		if (isProjectile)
			subfolder = "Projectiles";

		string selectedModifier = ModifierList.GetItemText(index);
		PathToSelectedModifierResource = "res://Custom Resources/" + subfolder + "/" + selectedModifier + "/";

		// TODO: Change _selectedModifierButton's texture to icon
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
}
