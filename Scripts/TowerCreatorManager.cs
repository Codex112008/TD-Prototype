using Godot;
using Godot.Collections;
using System;

public partial class TowerCreatorManager : Node2D
{
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	private Dictionary<Stat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<Effect> _selectedEffects;
	private Tower towerToCreate;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		towerToCreate = new Tower();
		
		HBoxContainer costPicker = _statPickerScene.Instantiate<HBoxContainer>();
		RichTextLabel costPickerText = costPicker.GetChild<RichTextLabel>(0); // Because the first child of the scene SHOULD be a richtextlabel
		costPickerText.Text = "Cost";
		SpinBox costPickerSpinBox = costPicker.GetChild<SpinBox>(1); // Again, the second child of the scene SHOULD be a spinbox
		costPickerSpinBox.Step = 50;
		costPickerSpinBox.MaxValue = 1000;
		_towerCreatorUI.AddChild(costPicker);

		for (int i = 0; i < Enum.GetNames(typeof(Stat)).Length; i++)
		{
			HBoxContainer statPicker = _statPickerScene.Instantiate<HBoxContainer>();
			RichTextLabel statPickerText = statPicker.GetChild<RichTextLabel>(0); // Because the first child of the scene SHOULD be a richtextlabel
			statPickerText.Text = Enum.GetName(typeof(Stat), i);
			// Special things if shotspersecond cuz i wanat it to be called firerate on display using the /s
			if ((Stat)i == Stat.ShotsPerSecond)
			{
				SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1); // Again, the second child of the scene SHOULD be a spinbox
				statPickerText.Text = "Fire Rate";
				statPickerSpinBox.Suffix = "/s";
			}
			_towerCreatorUI.AddChild(statPicker);
		}

		HBoxContainer projectilePicker = _modifierPickerScene.Instantiate<HBoxContainer>();
		projectilePicker.GetChild<RichTextLabel>(0).Text = "Projectile"; // Because the first child of the scene SHOULD be a richtextlabel
		OptionButton projectilePickerOptionButton = projectilePicker.GetChild<OptionButton>(1);
		foreach (string projectileName in GetFolderNames("res://Custom Resources/Projectiles/"))
			projectilePickerOptionButton.AddItem(projectileName);
		_towerCreatorUI.AddChild(projectilePicker);

		for (int i = 0; i < _selectedEffects.Count; i++)
		{
			HBoxContainer effectPicker = _modifierPickerScene.Instantiate<HBoxContainer>();
			effectPicker.GetChild<RichTextLabel>(0).Text = "Effect " + (i + 1); // Because the first child of the scene SHOULD be a richtextlabel
			OptionButton effectPickerOptionButton = effectPicker.GetChild<OptionButton>(1);
			foreach (string effectName in GetFolderNames("res://Custom Resources/Effects/"))
				effectPickerOptionButton.AddItem(effectName);
			_towerCreatorUI.AddChild(effectPicker);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public Array<string> GetFolderNames(string path)
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
