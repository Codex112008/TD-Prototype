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
	
	[Export] private Dictionary<TowerStat, int> _defaultStats;
	[Export] private PackedScene _towerTypeScene;
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
		_towerNameInput.Text = SplitIntoPascalCase(_towerTypeScene.ResourcePath[(_towerTypeScene.ResourcePath.LastIndexOf('/') + 1).._towerTypeScene.ResourcePath.LastIndexOf(".tscn")]);

		_towerToCreatePreview = _towerTypeScene.Instantiate<Tower>();
		_towerToCreatePreview.GlobalPosition = _towerPreviewArea.MapToLocal(new Vector2I(9, 4)) - _towerPreviewArea.TileSet.TileSize / 2;
		_towerPreviewArea.AddChild(_towerToCreatePreview);

		for (int i = 0; i < Enum.GetNames(typeof(TowerStat)).Length; i++)
		{
			TowerStat stat = (TowerStat)i;

			HBoxContainer statPicker = InstantiateStatSelector(Enum.GetName(typeof(TowerStat), stat));
			SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1);
			statPickerSpinBox.Value = _defaultStats[stat];

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

		for (int i = 0; i < _towerLevel + 1; i++)
		{
			InstantiateModifierSelector("Effect " + (i + 1), "res://Custom Resources/Effects/");
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

		_totalTowerCostLabel.Text = "Point Usage: " + _towerToCreatePreview.GetCurrentTotalPointsAllocated() + "/" + _towerToCreatePreview.GetMaximumPointsFromCost();
		if (_towerToCreatePreview.GetCurrentTotalPointsAllocated() > _towerToCreatePreview.GetMaximumPointsFromCost())
			_totalTowerCostLabel.Text += "\nCost exceeds maximum by " + (_towerToCreatePreview.GetCurrentTotalPointsAllocated() - _towerToCreatePreview.GetMaximumPointsFromCost()) + " points";
	}

	public void SaveTowerResource()
	{
		if (_towerToCreatePreview.HasValidPointAllocation())
		{
			GD.Print("Successfully created tower!");
		}
		else
		{
			GD.Print("Ur tower to op :skull:");
			return;
		}

		PackedScene towerToSave = new();
		Error packResult = towerToSave.Pack(_towerToCreatePreview);

		if (towerToSave != null && packResult == Error.Ok)
		{
			Error saveResult = ResourceSaver.Save(towerToSave, "res://SavedTowers/" + RemoveWhitespaces(_towerNameInput.Text) + ".tscn");
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
}
