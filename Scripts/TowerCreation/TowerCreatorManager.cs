using Godot;
using Godot.Collections;
using System;

public partial class TowerCreatorManager : Node2D
{
	[Export] private PackedScene towerTypeScene;
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	[Export] private int towerLevel = 0;
	private Dictionary<Stat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<Effect> _selectedEffects;
	private Tower towerToCreatePreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		towerToCreatePreview = towerTypeScene.Instantiate<Tower>();
		// TODO: move towertocreate when i make the preview area to the preview place thing idk

		HBoxContainer costPicker = InstantiateStatSelector("Cost");
		SpinBox costPickerSpinBox = costPicker.GetChild<SpinBox>(1); // The second child of the scene SHOULD be a spinbox
		costPickerSpinBox.Step = 50;
		costPickerSpinBox.MaxValue = 1000;

		for (int i = 0; i < Enum.GetNames(typeof(Stat)).Length; i++)
		{
			Stat stat = (Stat)i;

			HBoxContainer statPicker = InstantiateStatSelector(Enum.GetName(typeof(Stat), stat));
			// Special things if shotspersecond cuz i wanat it using the /s
			if (stat == Stat.FireRate)
			{
				SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1); // The second child of the scene SHOULD be a spinbox
				statPickerSpinBox.Suffix = "/s";
			}
		}

		InstantiateModifierSelector("Projectile", "res://Custom Resources/Projectiles/");

		for (int i = 0; i < towerLevel + 1; i++)
		{
			InstantiateModifierSelector("Effect " + (i + 1), "res://Custom Resources/Effects/");
		}

		_towerCreatorUI.MoveChild(_towerCreatorUI.GetChild(0), -1); // Moves the save button to the last index, so appears last in container
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	private Tower GenerateTowerFromSelection()
	{
		// TODO: Test this line i hope for the love of god this works
		Tower wipTower = (Tower)Activator.CreateInstance(Type.GetType(towerToCreatePreview.GetType().Name));

		Array<Effect> effects = new Array<Effect>();
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i);
			if (pickerNodeType is HBoxContainer statPicker)
			{
				Stat stat = (Stat)Enum.Parse(typeof(Stat), statPicker.GetChild<RichTextLabel>(0).Text);
				wipTower.TowerStats[stat] = Mathf.RoundToInt(statPicker.GetChild<SpinBox>(1).Value);
			}
			else if (pickerNodeType is ModifierSelector modifierPicker)
			{
				bool isProjectile = pickerNodeType.GetParent().GetChild<RichTextLabel>(0).Text.Contains("Projectile");

				if (isProjectile)
					wipTower.Projectile = ResourceLoader.Load<Projectile>(modifierPicker.PathToSelectedModifierResource);
				else
					effects.Add(ResourceLoader.Load<Effect>(modifierPicker.PathToSelectedModifierResource));
			}
		}
		wipTower.SetEffects(effects);

		if (wipTower.HasValidPointAllocation())
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
		PackedScene towerToSave = new();
		Error packResult = towerToSave.Pack(GenerateTowerFromSelection());

		if (towerToSave != null && packResult == Error.Ok)
		{
			Error saveResult = ResourceSaver.Save(towerToSave, "res://SavedTowers/");
			GD.Print(saveResult);
		}
		else
			GD.Print("Smth went wrong xd");
	}

	private ModifierSelector InstantiateModifierSelector(string modifierSelectorLabelName, string pathToModifiers)
	{
		ModifierSelector modifierPicker = _modifierPickerScene.Instantiate<ModifierSelector>();
		modifierPicker.PathToModifiers = pathToModifiers;
		modifierPicker.ModifierLabel.Text = modifierSelectorLabelName;
		modifierPicker.UpdateModifierSelector();

		_towerCreatorUI.AddChild(modifierPicker);

		return modifierPicker;
	}

	private StatSelector InstantiateStatSelector(string statSelectorLabelName)
	{
		StatSelector statPicker = _statPickerScene.Instantiate<StatSelector>();
		statPicker.StatLabel.Text = statSelectorLabelName;
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}
}
