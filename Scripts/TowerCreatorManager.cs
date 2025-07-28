using Godot;
using Godot.Collections;
using System;

public partial class TowerCreatorManager : Node2D
{
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	[Export] private int towerLevel = 0;
	private Dictionary<Stat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<Effect> _selectedEffects;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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
			// Special things if shotspersecond cuz i wanat it using the /s
			if ((Stat)i == Stat.FireRate)
			{
				SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1); // Again, the second child of the scene SHOULD be a spinbox
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

		for (int i = 0; i < towerLevel + 1; i++)
		{
			HBoxContainer effectPicker = _modifierPickerScene.Instantiate<HBoxContainer>();
			effectPicker.GetChild<RichTextLabel>(0).Text = "Effect " + (i + 1); // Because the first child of the scene SHOULD be a richtextlabel
			OptionButton effectPickerOptionButton = effectPicker.GetChild<OptionButton>(1);
			foreach (string effectName in GetFolderNames("res://Custom Resources/Effects/"))
				effectPickerOptionButton.AddItem(effectName);
			_towerCreatorUI.AddChild(effectPicker);
		}

		_towerCreatorUI.MoveChild(_towerCreatorUI.GetChild(0), -1); // Moves the save button to the last index, so appears last in container
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private Tower GenerateTowerFromSelection()
	{
		Tower wipTower = new Tower();

		Array<Effect> effects = new Array<Effect>();
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i).GetChild(1);
			if (pickerNodeType is SpinBox spinBox)
			{
				Stat stat = (Stat)Enum.Parse(typeof(Stat), pickerNodeType.GetParent().GetChild<RichTextLabel>(0).Text);
				wipTower.TowerStats[stat] = Mathf.RoundToInt(spinBox.Value);
			}
			else if (pickerNodeType is OptionButton optionButton)
			{
				using DirAccess dir = DirAccess.Open("res://Custom Resources/");
				if (pickerNodeType.GetParent().GetChild<RichTextLabel>(0).Text.Contains("Projectile"))
				{
					dir.ChangeDir(dir.GetCurrentDir(false) + "Projectiles/" + optionButton.GetItemText(optionButton.Selected) + "/");
					foreach (string file in dir.GetFiles())
					{
						if (file.Contains("Resource"))
						{
							Projectile selectedProjectile = ResourceLoader.Load<Projectile>(dir.GetCurrentDir(false) + file + "/");
							wipTower.Projectile = selectedProjectile;
						}
					}
				}
				else
				{
					dir.ChangeDir(dir.GetCurrentDir(false) + "Effects/" + optionButton.GetItemText(optionButton.Selected) + "/");
					foreach (string file in dir.GetFiles())
					{
						if (file.Contains("Resource"))
						{
							Effect selectedEffect = ResourceLoader.Load<Effect>(dir.GetCurrentDir(false) + file + "/");
							effects.Add(selectedEffect);
						}
					}
				}
			}
		}
		wipTower.SetEffects(effects);

		if (wipTower.IsPointAllocationValid())
		{
			GD.Print("Successfully created tower!");
			return wipTower;
		}
		else
		{
			GD.Print("Unsuccessfully created tower!");
			return null;
		}
	}

	public void SaveTowerResource()
	{
		Tower towerToSave = GenerateTowerFromSelection();

		if (towerToSave != null)
		{
			Error result = ResourceSaver.Save(towerToSave, "res://SavedTowers/");
			GD.Print(result);
		}
		else
			GD.Print("Your tower is too op lmao");
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
