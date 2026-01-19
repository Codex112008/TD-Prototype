using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class TowerCreatorController : Node2D
{
	public static TowerCreatorController instance;
	public override void _EnterTree()
	{
		instance = this;
	}

	[Export] public Array<Projectile> ProjectileOptions = [];
	[Export] public Array<TowerEffect> EffectOptions = [];
	[Export] public Array<PackedScene> TowerTypeScenes = [];

	[Export] private string _savedTowerFilePath = "RuntimeData/SavedTowers/";
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private TileMapLayer _towerPreviewArea;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;

	public bool IsMaxTowersCreated;

	private Dictionary<TowerStat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<TowerEffect> _selectedEffects;
	private LineEdit _towerNameInput;
	private RichTextLabel _totalTowerCostLabel;
	private TowerColorPickerButton _towerColorPickerButton;
	private TowerSelector _towerSelector;
	private Tower _towerToCreatePreview;
	private Button _saveButton;

	// Used for tower upgrading
	[Export] public PackedScene BaseTowerScene;
	private bool _isUpgrading;
	private int _towerLevel = 0;
	private Tower _baseTowerInstance;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Upgrading init
		_isUpgrading = BaseTowerScene != null;
		if (_isUpgrading)
		{
			_baseTowerInstance = BaseTowerScene.Instantiate<Tower>();
			_baseTowerInstance.TowerLevel = _towerLevel;
		}

		_savedTowerFilePath = Utils.AddCorrectDirectoryToPath(_savedTowerFilePath);
		if (!DirAccess.DirExistsAbsolute(_savedTowerFilePath))
			DirAccess.MakeDirRecursiveAbsolute(_savedTowerFilePath);

		// Sets tower level if upgrading tower
		if (_isUpgrading)
			_towerLevel = (int)char.GetNumericValue(BaseTowerScene.ResourcePath[BaseTowerScene.ResourcePath.LastIndexOf('/')..BaseTowerScene.ResourcePath.LastIndexOf('.')][^1]) + 1;

		// Creates a preview of the tower being created
		_towerSelector = _towerCreatorUI.GetChild<TowerSelector>(3);
		if (_isUpgrading)
			InstantiateTowerPreview(BaseTowerScene);
		else
			InstantiateTowerPreview(TowerTypeScenes[0]);

		// Gets the name editor and defaults it to the base scene name (if existing tower uses that name asw)
		_towerNameInput = _towerCreatorUI.GetChild<LineEdit>(1);
		_towerNameInput.Text = Utils.SplitPascalCase(_towerToCreatePreview.Name);
		if (_isUpgrading) // Cant change name if its an upgraded tower
		{
			_towerNameInput.Editable = false;
			_towerNameInput.Text = Utils.SplitPascalCase(_towerToCreatePreview.TowerName);
		}

		// Sets default color
		_towerColorPickerButton = _towerCreatorUI.GetChild<TowerColorPickerButton>(2);
		if (_towerToCreatePreview.SpritesToColor.Count > 0)
			_towerColorPickerButton.Color = _towerToCreatePreview.SpritesToColor[0].SelfModulate;
		else
			_towerColorPickerButton.QueueFree();

		// Init tower creator
		_towerSelector.UpdateSelector();
		if (_isUpgrading)
		{
			_towerSelector.DisableSelector();

			int index = _towerSelector.GetIndexFromText(_towerToCreatePreview.GetType().Name);
			_towerSelector.ItemList.Select(index);
			_towerSelector.OnItemSelected(index);
		}
		else
		{
			_towerSelector.ItemList.Select(0);
			_towerSelector.OnItemSelected(0);
		}
		
		// Creates all the stat pickers
		for (int i = 0; i < Enum.GetNames(typeof(TowerStat)).Length; i++)
		{
			TowerStat stat = (TowerStat)i;

			HBoxContainer statPicker = InstantiateStatSelector(Enum.GetName(typeof(TowerStat), stat));
			SpinBox statPickerSpinBox = statPicker.GetChild<SpinBox>(1);

			statPickerSpinBox.MinValue = 0;

			switch (stat)
			{
				case TowerStat.Cost:
					statPickerSpinBox.MaxValue = 1000 * Mathf.RoundToInt(Mathf.Pow(10, _towerLevel));
					if (_isUpgrading)
						statPickerSpinBox.MinValue = 50;
					statPickerSpinBox.Editable = false;
					break;
				case TowerStat.Range:
					statPickerSpinBox.Step = 5;
					break;
				case TowerStat.FireRate:
					statPickerSpinBox.Suffix = "/s";
					statPickerSpinBox.CustomMinimumSize = new(90f, 0f);
					break;
			}

			if (!_isUpgrading)
				statPickerSpinBox.Value = _towerToCreatePreview.BaseTowerStats[stat];
		}

		// Creates the Modifier Selectors
		InstantiateModifierSelector("Projectile");
		for (int i = 0; i < Mathf.Min(_towerLevel + 1, 3); i++)
			InstantiateModifierSelector("Effect", i);

		// Creates the label showing the total and used cost
		_totalTowerCostLabel = new RichTextLabel
		{
			Theme = _towerCreatorUI.Theme,
			FitContent = true,
			AutowrapMode = TextServer.AutowrapMode.Off,
			CustomMinimumSize = Vector2.Down * 36
		};
		_towerCreatorUI.AddChild(_totalTowerCostLabel);

		_saveButton = _towerCreatorUI.GetChild<Button>(0);
		_towerCreatorUI.MoveChild(_saveButton, -1); // Moves the save button to the last index, so appears last in container

		UpdateTowerPreview();

		// If no tower slots available, disable save button and display message on screen "No tower slots available, edit existing towers"
		if (BuildingManager.instance.IsMaxTowersCreated() && !_isUpgrading)
		{
			_saveButton.Disabled = true;
			_saveButton.Text = "No tower slots available";
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateTowerPreview(string _ = "")
	{
		// if performing an upgrade reset preview tower stats to base tower stats before updating them
		if (_isUpgrading)
    		_towerToCreatePreview.BaseTowerStats = new Dictionary<TowerStat, int>(_baseTowerInstance.BaseTowerStats);

		// If the tower type is different
		bool newTowerType = false;
		if (_towerToCreatePreview.GetType().Name != _towerSelector.SelectedTowerTypeName())
		{
			if (_towerNameInput.Text == Utils.SplitPascalCase(_towerToCreatePreview.GetType().Name))
				_towerNameInput.Text = Utils.SplitPascalCase(_towerSelector.SelectedTowerTypeName());

			_towerToCreatePreview.QueueFree();
			InstantiateTowerPreview(_towerSelector.SelectedTowerType, false);

			if (_towerColorPickerButton != null && _towerToCreatePreview.SpritesToColor != null && _towerToCreatePreview.SpritesToColor.Count > 0)
				_towerColorPickerButton.Color = _towerToCreatePreview.SpritesToColor[0].SelfModulate;

			newTowerType = true;
		}

		if (IsInstanceValid(_towerToCreatePreview.InstancedProjectiles))
		{
			foreach(Node node in _towerToCreatePreview.InstancedProjectiles.GetChildren())
				node.QueueFree();
		}

		StatSelector costSelector = null;
		Array<StatSelector> otherStatSelectors = [];
		Array<TowerEffect> effects = [];
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i);
			if (pickerNodeType is StatSelector statSelector)
			{
				// Updates stat picker text and sets it on the preview
				TowerStat stat = (TowerStat)Enum.Parse(typeof(TowerStat), Utils.RemoveWhitespaces(statSelector.StatLabel.Text));
				UpdateTowerPreviewStat(statSelector, stat);
				if (stat == TowerStat.Cost)
					costSelector = statSelector;
				else
					otherStatSelectors.Add(statSelector);
			}
			else if (pickerNodeType is ModifierSelector modifierPicker)
			{
				// Updates modifier selector text and sets it on the preview
				TowerComponent towerComponent;
				if (modifierPicker.ItemLabel.Text.Contains("Projectile"))
					towerComponent = ProjectileOptions[modifierPicker.ItemList.GetSelectedItems()[0]];
				else
					towerComponent = EffectOptions[modifierPicker.ItemList.GetSelectedItems()[0]];

				if (towerComponent is Projectile projectile)
					_towerToCreatePreview.Projectile = projectile;
				else if (towerComponent is TowerEffect effect)
					effects.Add(effect);

				modifierPicker.CostLabel.Text = towerComponent.ResourceName/*"Cost: " + towerComponent.PointCost*/;
				modifierPicker.SelectedItemTextureButton.TooltipText = towerComponent.Tooltip;
			}
		}
		_towerToCreatePreview.SetEffects(effects);

		// Colors the tower preview based on color picker
		if (_towerColorPickerButton != null)
		{
			foreach (Sprite2D sprite in _towerToCreatePreview.SpritesToColor)
			{
				sprite.SelfModulate = _towerColorPickerButton.Color;
			}
		}

		// Change name of the node
		_towerToCreatePreview.TowerName = _towerNameInput.Text;

		// Auto sets cost spinbox
		if (costSelector != null)
		{
			costSelector.StatSpinBox.Value = CalculateCurrentTotalPointsAllocated();
			UpdateTowerPreviewStat(costSelector, TowerStat.Cost);
			costSelector.CostLabel.Text = "($" + _towerToCreatePreview.GetFinalTowerStats()[TowerStat.Cost] + ")";
		}

		foreach (StatSelector statSelector in otherStatSelectors)
			statSelector.CostLabel.Text = "(" + _towerToCreatePreview.GetFinalTowerStats()[(TowerStat)Enum.Parse(typeof(TowerStat), Utils.RemoveWhitespaces(statSelector.StatLabel.Text))] + ")";

		// Updates the point usage label and gives a warning if exceeding it
		if (_totalTowerCostLabel != null)
		{
			_totalTowerCostLabel.Text = "Point Usage: " + CalculateCurrentTotalPointsAllocated() + "/" + CalculateMaximumPoints();
			if (CalculateCurrentTotalPointsAllocated() > CalculateMaximumPoints())
				_totalTowerCostLabel.Text += "\nCost exceeds maximum by " + (CalculateCurrentTotalPointsAllocated() - CalculateMaximumPoints()) + " points";
		}

		if (newTowerType)
			_towerPreviewArea.AddChild(_towerToCreatePreview);

		_towerToCreatePreview.TowerLevel = _towerLevel;
	}

	public void UpdateTowerPreviewStat(StatSelector statSelector, TowerStat stat)
	{
		if (_isUpgrading && stat != TowerStat.Cost)
			_towerToCreatePreview.BaseTowerStats[stat] = _baseTowerInstance.BaseTowerStats[stat] + Mathf.RoundToInt(statSelector.StatSpinBox.Value);
		else
			_towerToCreatePreview.BaseTowerStats[stat] = Mathf.RoundToInt(statSelector.StatSpinBox.Value);

		int pointCost = CalculatePointCostForStat(stat);
		// if (stat != TowerStat.Cost)
		//	statSelector.CostLabel.Text = "(" + _towerToCreatePreview.GetFinalTowerStats()[stat] + ")";
		// else
		// 	statSelector.CostLabel.Text = "Max Points: " + pointCost;
	}

	public void SaveTowerResource()
	{
		// Only allow tower creation if valid point allocation
		if (CalculateCurrentTotalPointsAllocated() <= CalculateMaximumPoints())
		{
			_saveButton.Text = "Saved " + _towerNameInput.Text + " Successfully!";
			GetTree().CreateTimer(1f).Connect(Timer.SignalName.Timeout, Callable.From(ResetSaveButtonText));
		}
		else
		{
			_saveButton.Text = _towerNameInput.Text + " is too strong!";
			GetTree().CreateTimer(1f).Connect(Timer.SignalName.Timeout, Callable.From(ResetSaveButtonText));
			return;
		}
		// Cant make tower if name already used
		if (FileAccess.FileExists(_savedTowerFilePath + Utils.RemoveWhitespaces(_towerNameInput.Text) + '/' + Utils.RemoveWhitespaces(_towerNameInput.Text) + _towerLevel + ".tscn"))
		{
			_saveButton.Text = "Tower named " + _towerNameInput.Text + " already exists!";
			GetTree().CreateTimer(1f).Connect(Timer.SignalName.Timeout, Callable.From(ResetSaveButtonText));
			return;
		}

		// Duplicates the tower preview to save temporaily so can change variables without changing the preview
		if (IsInstanceValid(_towerToCreatePreview.InstancedProjectiles))
			_towerToCreatePreview.InstancedProjectiles.Free();
		Tower towerToSave = (Tower)_towerToCreatePreview.Duplicate();
		AddToGroup("Persist", true);
		towerToSave.RangeAlwaysVisible = false;
		towerToSave.Position = Vector2.Zero;
		foreach (Node node in towerToSave.GetChildren())
		{
			if (node is Node2D node2D)
			{
				node2D.Rotation = 0;
			}
		}

		// Packs duplicated tower scene into a PackedScene to save
		PackedScene towerToSaveScene = new();
		Error packResult = towerToSaveScene.Pack(towerToSave);

		/* Cutting out modification of towers
		// If modifying tower then remove old version
		if (BaseTowerScene != null)
			Utils.RemoveDirRecursive(BaseTowerScene.ResourcePath[..BaseTowerScene.ResourcePath.LastIndexOf('/')]);
		*/

		if (towerToSave != null && packResult == Error.Ok)
		{
			DirAccess dirAccess = DirAccess.Open(_savedTowerFilePath);
			if (dirAccess != null)
			{
				// Checks if a folder for this tower exists and makes one if not
				if (!dirAccess.DirExists(Utils.RemoveWhitespaces(_towerNameInput.Text)))
					dirAccess.MakeDir(Utils.RemoveWhitespaces(_towerNameInput.Text));
				dirAccess.ChangeDir(Utils.RemoveWhitespaces(_towerNameInput.Text));

				// Saves tower to the correct folder
				ResourceSaver.Save(towerToSaveScene, dirAccess.GetCurrentDir() + "/" + Utils.RemoveWhitespaces(_towerNameInput.Text) + _towerLevel + ".tscn");

				// Gets every sprite under the tower and itself to convert into a image to save to the same folder as scene
				Image towerAsImage = Utils.CreateImageFromSprites(towerToSave);
				towerAsImage?.SavePng(dirAccess.GetCurrentDir() + "/" + Utils.RemoveWhitespaces(_towerNameInput.Text) + _towerLevel + "Icon.png");
			}
		}
		else
			GD.Print("Smth went wrong");

		towerToSave.Free();
	}

	private ModifierSelector InstantiateModifierSelector(string modifierSelectorLabelName, int number = -1)
	{
		ModifierSelector modifierSelector = _modifierPickerScene.Instantiate<ModifierSelector>();
		bool IsProjectile = number == -1;

		if (!IsProjectile) // If has a number then its an effect
		{
			modifierSelector.ItemLabel.Text = modifierSelectorLabelName + " " + (number + 1);
			modifierSelector.ModifiersToDisplay = [.. EffectOptions.Cast<TowerComponent>()];
		}
		else
		{
			modifierSelector.ItemLabel.Text = modifierSelectorLabelName;
			
			// If upgrading limit projectiles to the upgrades, and if none exist disable the picker and if not upgrading then just keep defaults
			if (_isUpgrading)
			{
				if (_baseTowerInstance.Projectile.NextTierProjectiles.Count == 0)
					ProjectileOptions = [_baseTowerInstance.Projectile];
				else
					ProjectileOptions = _baseTowerInstance.Projectile.NextTierProjectiles;
			}

			modifierSelector.ModifiersToDisplay = [.. ProjectileOptions.Cast<TowerComponent>()];
		}

		modifierSelector.UpdateSelector();

		// Disables effects that are use in previous tower levels
		if (IsProjectile && _towerToCreatePreview.Projectile != null && !_isUpgrading)
		{
			int index = modifierSelector.GetIndexFromText(modifierSelector.ModifiersToDisplay.FirstOrDefault(modifier => modifier.ResourceName == _towerToCreatePreview.Projectile.ResourceName).ResourceName);
			modifierSelector.ItemList.Select(index);
			modifierSelector.OnItemSelected(index);
		}
		else if (!IsProjectile && _isUpgrading && number < _towerLevel)
		{
			// Cant modify old effects
			modifierSelector.DisableSelector();

			int index = modifierSelector.GetIndexFromText(_baseTowerInstance.Projectile.Effects[number].ResourceName);
			modifierSelector.ItemList.Select(index);
			modifierSelector.OnItemSelected(index);
		}
		else
		{
			if (!IsProjectile && _isUpgrading)
			{
				foreach (TowerEffect effect in _baseTowerInstance.Projectile.Effects)
					modifierSelector.ItemList.SetItemDisabled(modifierSelector.GetIndexFromText(effect.ResourceName), true);
			}

			for (int i = 0; i < modifierSelector.ItemList.ItemCount; i++)
			{
				if (!modifierSelector.ItemList.IsItemDisabled(i))
				{
					modifierSelector.ItemList.Select(i);
					modifierSelector.OnItemSelected(i);
					break;
				}
			}
		}

		_towerCreatorUI.AddChild(modifierSelector);

		return modifierSelector;
	}

	private StatSelector InstantiateStatSelector(string statSelectorLabelName)
	{
		StatSelector statPicker = _statPickerScene.Instantiate<StatSelector>();
		statPicker.StatLabel.Text = Utils.SplitPascalCase(statSelectorLabelName);
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}

	private void InstantiateTowerPreview(PackedScene towerType, bool addToScene = true)
	{
		_towerToCreatePreview = towerType.Instantiate<Tower>();
		_towerSelector.SelectedItemTextureButton.TooltipText = _towerToCreatePreview.Tooltip;
		_towerToCreatePreview.GlobalPosition = new Vector2I(14, 6) * PathfindingManager.instance.TileSize;
		_towerToCreatePreview.RangeAlwaysVisible = true;
		if (addToScene)
			_towerPreviewArea.AddChild(_towerToCreatePreview);
	}

	private void ResetSaveButtonText()
	{
		_saveButton.Text = "Save";
	}

	private int CalculateCurrentTotalPointsAllocated()
	{
		if (_isUpgrading)
			return _towerToCreatePreview.GetCurrentTotalPointsAllocated() - _baseTowerInstance.GetCurrentTotalPointsAllocated();

		return _towerToCreatePreview.GetCurrentTotalPointsAllocated();
	}

	private int CalculateMaximumPoints()
    {
        return _towerToCreatePreview.GetMaximumPointsFromCost();
    }

	private int CalculatePointCostForStat(TowerStat stat)
    {
		if (_isUpgrading)
			return _towerToCreatePreview.GetPointCostForStat(stat) - _baseTowerInstance.GetPointCostForStat(stat);

        return _towerToCreatePreview.GetPointCostForStat(stat);
    }
}