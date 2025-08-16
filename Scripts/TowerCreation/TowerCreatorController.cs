using Godot;
using Godot.Collections;
using System;
using System.Text.RegularExpressions;

public partial class TowerCreatorController : Node2D
{
	public static TowerCreatorController instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one TowerCreator in scene!");
			return;
		}
		instance = this;
	}

	[Export] private string _savedTowerFilePath = "res://RuntimeData/SavedTowers/";
	[Export] private PackedScene _baseTowerScene;
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private TileMapLayer _towerPreviewArea;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	[Export] private int _towerLevel = 0;
	private Dictionary<TowerStat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<TowerEffect> _selectedEffects;
	private TextEdit _towerNameInput;
	private RichTextLabel _totalTowerCostLabel;
	private Tower _towerToCreatePreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_towerNameInput = _towerCreatorUI.GetChild<TextEdit>(1);
		_towerNameInput.Text = SplitIntoPascalCase(_baseTowerScene.ResourcePath[(_baseTowerScene.ResourcePath.LastIndexOf('/') + 1).._baseTowerScene.ResourcePath.LastIndexOf(".tscn")]);

		_towerToCreatePreview = _baseTowerScene.Instantiate<Tower>();
		_towerToCreatePreview.GlobalPosition = new Vector2I(9, 4) * 64;
		_towerToCreatePreview.RangeAlwaysVisible = true;
		_towerPreviewArea.AddChild(_towerToCreatePreview);

		for (int i = 0; i < Enum.GetNames(typeof(TowerStat)).Length; i++)
		{
			TowerStat stat = (TowerStat)i;

			HBoxContainer statPicker = InstantiateStatSelector(Enum.GetName(typeof(TowerStat), stat));
			SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1);
			statPickerSpinBox.Value = _towerToCreatePreview.BaseTowerStats[stat];

			switch (stat)
			{
				case TowerStat.Cost:
					statPickerSpinBox.Step = 50;
					statPickerSpinBox.MaxValue = 1000;
					break;
				case TowerStat.Range:
					statPickerSpinBox.Step = 5;
					break;
				case TowerStat.FireRate:
					statPickerSpinBox.Suffix = "/s";
					break;
			}

			statPickerSpinBox.Value = _towerToCreatePreview.BaseTowerStats[stat];
		}

		InstantiateModifierSelector("Projectile", "res://Custom Resources/Projectiles/");

		for (int i = 0; i < _towerLevel + 1; i++)
		{
			InstantiateModifierSelector("Effect", "res://Custom Resources/Effects/", i);
		}

		_totalTowerCostLabel = new RichTextLabel
		{
			Theme = _towerCreatorUI.Theme,
			FitContent = true,
			AutowrapMode = TextServer.AutowrapMode.Off,
			CustomMinimumSize = Vector2.Down * 36
		};
		_towerCreatorUI.AddChild(_totalTowerCostLabel);

		_towerCreatorUI.MoveChild(_towerCreatorUI.GetChild(0), -1); // Moves the save button to the last index, so appears last in container

		UpdateTowerPreview();
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
				_towerToCreatePreview.BaseTowerStats[stat] = Mathf.RoundToInt(statPicker.StatSpinBox.Value);
				if (stat != TowerStat.Cost)
				{
					int statCost = _towerToCreatePreview.GetPointCostForStat(stat);
					statPicker.CostLabel.Text = "Cost: " + statCost;
				}
				else
				{
					int max = _towerToCreatePreview.GetPointCostForStat(stat);
					statPicker.CostLabel.Text = "Max Points: " + max;
				}
			}
			else if (pickerNodeType is ModifierSelector modifierPicker)
			{
				TowerComponent towerComponent = ResourceLoader.Load<TowerComponent>(modifierPicker.PathToSelectedModifierResource);

				if (towerComponent is Projectile projectile)
					_towerToCreatePreview.Projectile = projectile;
				else if (towerComponent is TowerEffect effect)
					effects.Add(effect);

				modifierPicker.CostLabel.Text = "Cost: " + towerComponent.PointCost;
			}
		}
		_towerToCreatePreview.SetEffects(effects);

		if (_totalTowerCostLabel != null)
		{
			_totalTowerCostLabel.Text = "Point Usage: " + _towerToCreatePreview.GetCurrentTotalPointsAllocated() + "/" + _towerToCreatePreview.GetMaximumPointsFromCost();
			if (_towerToCreatePreview.GetCurrentTotalPointsAllocated() > _towerToCreatePreview.GetMaximumPointsFromCost())
				_totalTowerCostLabel.Text += "\nCost exceeds maximum by " + (_towerToCreatePreview.GetCurrentTotalPointsAllocated() - _towerToCreatePreview.GetMaximumPointsFromCost()) + " points";
		}
	}

	public void SaveTowerResource()
	{
		Tower towerToSave = (Tower)_towerToCreatePreview.Duplicate();
		towerToSave.RangeAlwaysVisible = false;
		towerToSave.Position = Vector2.Zero;

		if (_towerToCreatePreview.HasValidPointAllocation())
		{
			GD.Print("Successfully created tower!");
		}
		else
		{
			GD.Print("Ur tower to op :skull:");
			return;
		}

		PackedScene towerToSaveScene = new();
		Error packResult = towerToSaveScene.Pack(towerToSave);

		if (towerToSave != null && packResult == Error.Ok)
		{
			Error saveResult = ResourceSaver.Save(towerToSaveScene, _savedTowerFilePath + RemoveWhitespaces(_towerNameInput.Text) + ".tscn");
			GD.Print(saveResult);
		}
		else
			GD.Print("Smth went wrong xd");

		towerToSave.Free();
	}

	private ModifierSelector InstantiateModifierSelector(string modifierSelectorLabelName, string pathToModifiers, int number = -1)
	{
		ModifierSelector modifierSelector = _modifierPickerScene.Instantiate<ModifierSelector>();

		modifierSelector.PathToModifiers = pathToModifiers;

		if (number != -1)
			modifierSelectorLabelName += " " + (number + 1);
		modifierSelector.ModifierLabel.Text = modifierSelectorLabelName;

		modifierSelector.UpdateModifierSelector();

		if (modifierSelectorLabelName.Contains("Projectile") && _towerToCreatePreview.Projectile != null)
		{
			SelectModifierIndexFromName(modifierSelector, _towerToCreatePreview.Projectile.ResourceName);
		}
		else if (modifierSelectorLabelName.Contains("Effect") && _towerToCreatePreview.Projectile.Effects.Count > number)
		{
			SelectModifierIndexFromName(modifierSelector, _towerToCreatePreview.Projectile.Effects[number].ResourceName);
		}
		else
		{
			modifierSelector.ModifierList.Select(0);
			modifierSelector.UpdatePathToSelectedModifierResource(0);
		}

		_towerCreatorUI.AddChild(modifierSelector);

		return modifierSelector;
	}

	private void SelectModifierIndexFromName(ModifierSelector modifierSelector, string modifierName)
	{
		for (int i = 0; i < modifierSelector.ModifierList.ItemCount; i++)
		{
			if (RemoveWhitespaces(modifierSelector.ModifierList.GetItemText(i)) == RemoveWhitespaces(modifierName))
			{
				modifierSelector.ModifierList.Select(i);
				modifierSelector.UpdatePathToSelectedModifierResource(i);
				break;
			}
		}
	}

	private StatSelector InstantiateStatSelector(string statSelectorLabelName)
	{
		StatSelector statPicker = _statPickerScene.Instantiate<StatSelector>();
		statPicker.StatLabel.Text = SplitIntoPascalCase(statSelectorLabelName);
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}

	// TODO: maybe move this to some util class
	private static readonly Regex sPascalCase = new("(?<!^)([A-Z])");
	public static string SplitIntoPascalCase(string input)
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

	public static Array<string> GetFolderNames(string path)
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
