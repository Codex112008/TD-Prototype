using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;

public partial class TowerCreatorManager : Node2D
{
	public static TowerCreatorManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one BuildManager in scene!");
			return;
		}
		instance = this;
	}

	[Export] private PackedScene towerTypeScene;
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private Node2D _towerPreviewArea;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	[Export] private int towerLevel = 0;
	private Dictionary<TowerStat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<TowerEffect> _selectedEffects;
	private Tower towerToCreatePreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		towerToCreatePreview = towerTypeScene.Instantiate<Tower>();
		_towerPreviewArea.AddChild(towerToCreatePreview);

		// TODO: move towertocreate when i make the preview area to the preview place thing idk

		for (int i = 0; i < Enum.GetNames(typeof(TowerStat)).Length; i++)
		{
			TowerStat stat = (TowerStat)i;

			HBoxContainer statPicker = InstantiateStatSelector(Enum.GetName(typeof(TowerStat), stat));
			SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1);

			if (stat == TowerStat.FireRate)
			{
				statPickerSpinBox.Suffix = "/s";
			}
			else if (stat == TowerStat.Cost)
			{
				statPickerSpinBox.Step = 50;
				statPickerSpinBox.MaxValue = 1000;
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

	public void UpdateTowerPreview(double _ = 0)
	{
		// TODO: Test this line i hope for the love of god this works
		//Tower wipTower = (Tower)Activator.CreateInstance(Type.GetType(towerToCreatePreview.GetType().Name));

		Array<TowerEffect> effects = [];
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i);
			if (pickerNodeType is StatSelector statPicker)
			{
				TowerStat stat = (TowerStat)Enum.Parse(typeof(TowerStat), RemoveWhitespaces(statPicker.StatLabel.Text));
				towerToCreatePreview.TowerStats[stat] = Mathf.RoundToInt(statPicker.StatSpinBox.Value);
				if (stat != TowerStat.Cost)
					statPicker.CostLabel.Text = "Cost: " + towerToCreatePreview.GetPointCostForStat(stat);
				else
					statPicker.CostLabel.Text = "Max Points: " + towerToCreatePreview.GetPointCostForStat(stat);
			}
			else if (pickerNodeType is ModifierSelector modifierPicker)
			{
				bool isProjectile = modifierPicker.ModifierLabel.Text.Contains("Projectile");
				if (isProjectile)
				{
					towerToCreatePreview.Projectile = ResourceLoader.Load<Projectile>(modifierPicker.PathToSelectedModifierResource);
				}
				else
				{
					effects.Add(ResourceLoader.Load<TowerEffect>(modifierPicker.PathToSelectedModifierResource));
				}

				modifierPicker.CostLabel.Text = "Cost: " + ResourceLoader.Load<TowerComponent>(modifierPicker.PathToSelectedModifierResource).PointCost;
			}
		}
		towerToCreatePreview.SetEffects(effects);
		GD.Print(towerToCreatePreview.Projectile.Effects);
	}

	public void SaveTowerResource()
	{
		if (towerToCreatePreview.HasValidPointAllocation())
		{
			GD.Print("Successfully created tower!");
		}
		else
		{
			GD.Print("Ur tower to op :skull:");
			return;
		}

		PackedScene towerToSave = new();
		Error packResult = towerToSave.Pack(towerToCreatePreview);
		GD.Print(towerToSave);

		if (towerToSave != null && packResult == Error.Ok)
		{
			Error saveResult = ResourceSaver.Save(towerToSave, "res://SavedTowers/newtower.tscn");
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
		statPicker.StatLabel.Text = SplitPascalCase(statSelectorLabelName);
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}

	// TODO: maybe move this to some util class
	private static readonly Regex sPascalCase = new("(?<!^)([A-Z])");
	public static string SplitPascalCase(string input)
	{
		// Inserts a space before each uppercase letter that is not the first character.
		// The pattern ensures that a space is inserted only if the uppercase letter
		// is preceded by a lowercase letter or another uppercase letter that is
		// part of an acronym (e.g., "GPSData" becomes "GPS Data").
		// IDFK how this works
		return sPascalCase.Replace(input, " $1").Trim();
	}

	public static string RemoveWhitespaces(string input)
	{
		return input.Replace(" ", "");
	}
}
