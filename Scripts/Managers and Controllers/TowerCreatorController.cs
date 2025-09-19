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

	[Export] public PackedScene BaseTowerScene;
	[Export] public Array<Projectile> ProjectileOptions = [];
	[Export] public Array<TowerEffect> EffectOptions = [];
	[Export] public Array<PackedScene> TowerTypeScenes = [];

	[Export] private string _savedTowerFilePath = "RuntimeData/SavedTowers/";
	[Export] private VBoxContainer _towerCreatorUI;
	[Export] private TileMapLayer _towerPreviewArea;
	[Export] private PackedScene _statPickerScene;
	[Export] private PackedScene _modifierPickerScene;
	[Export] private int _towerLevel = 0;

	public bool IsMaxTowersCreated;

	private Dictionary<TowerStat, float> _selectedStats;
	private Projectile _selectedProjectile;
	private Array<TowerEffect> _selectedEffects;
	private LineEdit _towerNameInput;
	private RichTextLabel _totalTowerCostLabel;
	private TowerColorPickerButton _towerColorPickerButton;
	private TowerSelector _towerSelector;
	private Tower _towerToCreatePreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_savedTowerFilePath = OS.HasFeature("editor") ? "res://" + _savedTowerFilePath : "user://" + _savedTowerFilePath;
		if (!DirAccess.DirExistsAbsolute(_savedTowerFilePath))
			DirAccess.MakeDirRecursiveAbsolute(_savedTowerFilePath);

		// Creates a preview of the tower being created
		if (BaseTowerScene != null)
			InstantiateTowerPreview(BaseTowerScene);
		else
			InstantiateTowerPreview(TowerTypeScenes[0]);

		// Gets the name editor and defaults it to the base scene name (if existing tower uses that name asw)
		_towerNameInput = _towerCreatorUI.GetChild<LineEdit>(1);
		_towerNameInput.Text = Utils.SplitIntoPascalCase(_towerToCreatePreview.Name);
		if (_towerLevel > 0) // Cant change name if its an upgraded tower
			_towerNameInput.Editable = false;

		// Sets default color
		_towerColorPickerButton = _towerCreatorUI.GetChild<TowerColorPickerButton>(2);
		if (_towerToCreatePreview.SpritesToColor.Count > 0)
			_towerColorPickerButton.Color = _towerToCreatePreview.SpritesToColor[0].SelfModulate;
		else
			_towerColorPickerButton.QueueFree();

		// Init tower creator
		_towerSelector = _towerCreatorUI.GetChild<TowerSelector>(3);
		_towerSelector.UpdateSelector();
		if (BaseTowerScene != null)
		{
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
			statPickerSpinBox.Value = _towerToCreatePreview.BaseTowerStats[stat];

			switch (stat)
			{
				case TowerStat.Cost:
					statPickerSpinBox.Step = 25;
					statPickerSpinBox.MaxValue = 1000;
					break;
				case TowerStat.Range:
					statPickerSpinBox.Step = 5;
					break;
				case TowerStat.FireRate:
					statPickerSpinBox.Suffix = "/s";
					statPickerSpinBox.CustomMinimumSize = new(90f, 0f);
					break;
			}
			statPickerSpinBox.Value = _towerToCreatePreview.BaseTowerStats[stat];
		}

		// Creates the Modifier Selectors
		InstantiateModifierSelector("Projectile");
		for (int i = 0; i < _towerLevel + 1; i++)
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

		Button saveButton = _towerCreatorUI.GetChild<Button>(0);
		_towerCreatorUI.MoveChild(saveButton, -1); // Moves the save button to the last index, so appears last in container

		UpdateTowerPreview();

		// If no tower slots available, disable save button and display message on screen "No tower slots available, edit existing towers"
		if (BuildingManager.instance.IsMaxTowersCreated())
		{
			saveButton.Disabled = true;
			saveButton.Text = "No tower slots available";
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void UpdateTowerPreview(string _ = "")
	{
		// If the tower type is different
		bool newTowerType = false;
		if (_towerToCreatePreview.GetType().Name != _towerSelector.SelectedTowerTypeName())
		{
			_towerToCreatePreview.QueueFree();
			InstantiateTowerPreview(_towerSelector.SelectedTowerType, false);
			newTowerType = true;
		}

		Array<TowerEffect> effects = [];
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i);
			if (pickerNodeType is StatSelector statSelector)
			{
				// Updates stat picker text and sets it on the preview
				TowerStat stat = (TowerStat)Enum.Parse(typeof(TowerStat), Utils.RemoveWhitespaces(statSelector.StatLabel.Text));
				UpdateTowerPreviewStat(statSelector, stat);

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

				modifierPicker.CostLabel.Text = "Cost: " + towerComponent.PointCost;
			}
		}
		_towerToCreatePreview.SetEffects(effects);

		if (_towerColorPickerButton != null)
		{
			foreach (Sprite2D sprite in _towerToCreatePreview.SpritesToColor)
			{
				sprite.SelfModulate = _towerColorPickerButton.Color;
			}
		}

		// Change name of the node
		_towerToCreatePreview.TowerName = _towerNameInput.Text;

		// Updates the point usage label and gives a warning if exceeding it
		if (_totalTowerCostLabel != null)
		{
			_totalTowerCostLabel.Text = "Point Usage: " + _towerToCreatePreview.GetCurrentTotalPointsAllocated() + "/" + _towerToCreatePreview.GetMaximumPointsFromCost();
			if (_towerToCreatePreview.GetCurrentTotalPointsAllocated() > _towerToCreatePreview.GetMaximumPointsFromCost())
				_totalTowerCostLabel.Text += "\nCost exceeds maximum by " + (_towerToCreatePreview.GetCurrentTotalPointsAllocated() - _towerToCreatePreview.GetMaximumPointsFromCost()) + " points";
		}

		if (newTowerType)
			_towerPreviewArea.AddChild(_towerToCreatePreview);
	}

	public void UpdateTowerPreviewStat(StatSelector statSelector, TowerStat stat)
	{
		_towerToCreatePreview.BaseTowerStats[stat] = Mathf.RoundToInt(statSelector.StatSpinBox.Value);

		if (stat != TowerStat.Cost)
		{
			int statCost = _towerToCreatePreview.GetPointCostForStat(stat);
			statSelector.CostLabel.Text = "Cost: " + statCost;
		}
		else
		{
			int max = _towerToCreatePreview.GetPointCostForStat(stat);
			statSelector.CostLabel.Text = "Max Points: " + max;
		}
	}

	public void SaveTowerResource()
	{
		// Only allow tower creation if valid point allocation
		if (_towerToCreatePreview.HasValidPointAllocation())
		{
			GD.Print("Successfully created tower!");
		}
		else
		{
			GD.Print("Ur tower to op :skull:");
			return;
		}

		// Duplicates the tower preview to save temporaily so can change variables without changing the preview
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

		// If modifying tower then remove old version
		if (BaseTowerScene != null)
			Utils.RemoveDirRecursive(BaseTowerScene.ResourcePath[..BaseTowerScene.ResourcePath.LastIndexOf('/')]);

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
				ResourceSaver.Save(towerToSaveScene, dirAccess.GetCurrentDir() + "/" + Utils.RemoveWhitespaces(_towerNameInput.Text) + ".tscn");

				// Gets every sprite under the tower and itself to convert into a image to save to the same folder as scene
				// Array<Sprite2D> towerSprites = [.. towerToSave.GetChildren(true).Where(child => child is Sprite2D).Cast<Sprite2D>()];
				// towerSprites.Insert(0, towerToSave);
				Image towerAsImage = Utils.CreateImageFromSprites(towerToSave);
				towerAsImage?.SavePng(dirAccess.GetCurrentDir() + "/" + Utils.RemoveWhitespaces(_towerNameInput.Text) + "Icon.png");
			}
		}
		else
			GD.Print("Smth went wrong xd");

		towerToSave.Free();
	}

	private ModifierSelector InstantiateModifierSelector(string modifierSelectorLabelName, int number = -1)
	{
		ModifierSelector modifierSelector = _modifierPickerScene.Instantiate<ModifierSelector>();
		bool IsProjectile = number != -1;

		if (!IsProjectile) // If has a number then its an effect
		{
			modifierSelector.ItemLabel.Text = "Effect " + (number + 1);
			modifierSelector.ModifiersToDisplay = [.. EffectOptions.Cast<TowerComponent>()];
		}
		else
		{
			modifierSelector.ItemLabel.Text = "Projectile";
			modifierSelector.ModifiersToDisplay = [.. ProjectileOptions.Cast<TowerComponent>()];
		}

		modifierSelector.UpdateSelector();

		if (IsProjectile && _towerToCreatePreview.Projectile != null)
		{
			int index = modifierSelector.GetIndexFromText(modifierSelector.ModifiersToDisplay.FirstOrDefault(modifier => modifier.ResourceName == _towerToCreatePreview.Projectile.ResourceName).ResourceName);
			modifierSelector.ItemList.Select(index);
			modifierSelector.OnItemSelected(index);
		}
		else if (modifierSelectorLabelName.Contains("Effect") && _towerToCreatePreview.Projectile.Effects.Count > number)
		{
			int index = modifierSelector.GetIndexFromText(modifierSelector.ModifiersToDisplay.FirstOrDefault(modifier => modifier.ResourceName == _towerToCreatePreview.Projectile.Effects[number].ResourceName).ResourceName);
			modifierSelector.ItemList.Select(index);
			modifierSelector.OnItemSelected(index);
		}
		else
		{
			modifierSelector.ItemList.Select(0);
			modifierSelector.OnItemSelected(0);
		}

		_towerCreatorUI.AddChild(modifierSelector);

		return modifierSelector;
	}

	private StatSelector InstantiateStatSelector(string statSelectorLabelName)
	{
		StatSelector statPicker = _statPickerScene.Instantiate<StatSelector>();
		statPicker.StatLabel.Text = Utils.SplitIntoPascalCase(statSelectorLabelName);
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}

	private void InstantiateTowerPreview(PackedScene towerType, bool addToScene = true)
	{
		_towerToCreatePreview = towerType.Instantiate<Tower>();
		_towerToCreatePreview.GlobalPosition = new Vector2I(11, 5) * PathfindingManager.instance.TileSize;
		_towerToCreatePreview.RangeAlwaysVisible = true;
		if (addToScene)
			_towerPreviewArea.AddChild(_towerToCreatePreview);
	}
}