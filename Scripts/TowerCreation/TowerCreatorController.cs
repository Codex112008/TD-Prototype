using Godot;
using Godot.Collections;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using static Godot.Image;

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
	private TowerColorPickerButton _towerColorPickerButton;
	private Tower _towerToCreatePreview;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		// Gets the name editor and defaults it to the base scene name (if existing tower uses that name asw)
		_towerNameInput = _towerCreatorUI.GetChild<TextEdit>(1);
		_towerNameInput.Text = SplitIntoPascalCase(_baseTowerScene.ResourcePath[(_baseTowerScene.ResourcePath.LastIndexOf('/') + 1).._baseTowerScene.ResourcePath.LastIndexOf(".tscn")]);
		if (_towerLevel > 0) // Cant change name if its an upgraded tower
			_towerNameInput.Editable = false;

		// Creates a preview of the tower being created
		_towerToCreatePreview = _baseTowerScene.Instantiate<Tower>();
		_towerToCreatePreview.GlobalPosition = new Vector2I(11, 5) * PathfindingManager.instance.TileSize;
		_towerToCreatePreview.RangeAlwaysVisible = true;
		_towerPreviewArea.AddChild(_towerToCreatePreview);

		_towerColorPickerButton = _towerCreatorUI.GetChild<TowerColorPickerButton>(2);
		if (_towerToCreatePreview.SpritesToColor.Count > 0)
			_towerColorPickerButton.Color = _towerToCreatePreview.SpritesToColor[0].SelfModulate;
		else
			_towerColorPickerButton.QueueFree();

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

		// Creates the Modifier Selectors
		InstantiateModifierSelector("Projectile", "res://Custom Resources/Projectiles/");
		for (int i = 0; i < _towerLevel + 1; i++)
			InstantiateModifierSelector("Effect", "res://Custom Resources/Effects/", i);

		// Creates the label showing the total and used cost
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

	public void UpdateTowerPreview()
	{
		Array<TowerEffect> effects = [];
		for (int i = 0; i < _towerCreatorUI.GetChildCount() - 1; i++)
		{
			Node pickerNodeType = _towerCreatorUI.GetChild(i);
			if (pickerNodeType is StatSelector statPicker)
			{
				// Updates stat picker text and sets it on the preview
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
				// Updates modifier selector text and sets it on the preview
				TowerComponent towerComponent = ResourceLoader.Load<TowerComponent>(modifierPicker.PathToSelectedModifierResource);

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

		// Updates the point usage label and gives a warning if exceeding it
		if (_totalTowerCostLabel != null)
		{
			_totalTowerCostLabel.Text = "Point Usage: " + _towerToCreatePreview.GetCurrentTotalPointsAllocated() + "/" + _towerToCreatePreview.GetMaximumPointsFromCost();
			if (_towerToCreatePreview.GetCurrentTotalPointsAllocated() > _towerToCreatePreview.GetMaximumPointsFromCost())
				_totalTowerCostLabel.Text += "\nCost exceeds maximum by " + (_towerToCreatePreview.GetCurrentTotalPointsAllocated() - _towerToCreatePreview.GetMaximumPointsFromCost()) + " points";
		}
	}

	public void SaveTowerResource()
	{
		// Duplicates the tower preview to save temporaily so can change variables without changing the preview
		Tower towerToSave = (Tower)_towerToCreatePreview.Duplicate();
		towerToSave.RangeAlwaysVisible = false;
		towerToSave.Position = Vector2.Zero;
		foreach (Node node in towerToSave.GetChildren())
		{
			if (node is Node2D node2D)
			{
				node2D.Rotation = 0;
			}
		}

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

		// Packs duplicated tower scene into a PackedScene to save
		PackedScene towerToSaveScene = new();
		Error packResult = towerToSaveScene.Pack(towerToSave);

		if (towerToSave != null && packResult == Error.Ok)
		{
			DirAccess dirAccess = DirAccess.Open(_savedTowerFilePath);
			if (dirAccess != null)
			{
				// Checks if a folder for this tower exists and makes one if not
				if (!dirAccess.DirExists(RemoveWhitespaces(_towerNameInput.Text)))
					dirAccess.MakeDir(RemoveWhitespaces(_towerNameInput.Text));
				dirAccess.ChangeDir(RemoveWhitespaces(_towerNameInput.Text));

				// Saves tower to the correct folder
				ResourceSaver.Save(towerToSaveScene, dirAccess.GetCurrentDir() + "/" + RemoveWhitespaces(_towerNameInput.Text) + ".tscn");

				// Gets every sprite under the tower and itself to convert into a image to save to the same folder as scene
				//Array<Sprite2D> towerSprites = [.. towerToSave.GetChildren(true).Where(child => child is Sprite2D).Cast<Sprite2D>()];
				//towerSprites.Insert(0, towerToSave);
				Image towerAsImage = CreateImageFromSprites(towerToSave);
				towerAsImage?.SavePng(dirAccess.GetCurrentDir() + "/" + RemoveWhitespaces(_towerNameInput.Text) + "Icon.png");
			}
		}
		else
			GD.Print("Smth went wrong xd");

		towerToSave.Free();
	}

	private ModifierSelector InstantiateModifierSelector(string modifierSelectorLabelName, string pathToModifiers, int number = -1)
	{
		ModifierSelector modifierSelector = _modifierPickerScene.Instantiate<ModifierSelector>();

		modifierSelector.PathToModifiers = pathToModifiers;

		if (number != -1) // If an effect then add number to label
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

	private StatSelector InstantiateStatSelector(string statSelectorLabelName)
	{
		StatSelector statPicker = _statPickerScene.Instantiate<StatSelector>();
		statPicker.StatLabel.Text = SplitIntoPascalCase(statSelectorLabelName);
		_towerCreatorUI.AddChild(statPicker);

		return statPicker;
	}

	// Gets index of modifier from the modifier selector given the modifier's name
	private static void SelectModifierIndexFromName(ModifierSelector modifierSelector, string modifierName)
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

	// Credits to random guy from the internet that had GDScript that I converted to C#
	public static Image CreateImageFromSprites(Tower towerToSave)
	{
		Array<Sprite2D> sprites = towerToSave.SpritesForIcon;
		if (sprites.Count > 1)
		{
			Format format = sprites[0].Texture.GetImage().GetFormat();
			Rect2 boundingBox = GetSpriteRect(sprites[0], towerToSave);
			for (int i = 1; i < sprites.Count; i++)
				boundingBox = boundingBox.Merge(GetSpriteRect(sprites[i], towerToSave));

			if (sprites.All(sprite => sprite.Texture.GetImage().GetFormat() == format) && boundingBox.Size.X > 0 && boundingBox.Size.Y > 0)
			{
				Image image = CreateEmpty((int)boundingBox.Size.X, (int)boundingBox.Size.Y, false, format);

				foreach (Sprite2D sprite in sprites)
				{
					Image spriteImage = sprite.Texture.GetImage();
					if (sprite.SelfModulate != Colors.White)
					{
						for (int i = 0; i < spriteImage.GetWidth(); i++)
						{
							for (int j = 0; j < spriteImage.GetHeight(); j++)
							{
								spriteImage.SetPixel(i, j, spriteImage.GetPixel(i, j) * sprite.SelfModulate);
							}
						}
					}

					image.BlendRect(spriteImage, new Rect2I(Vector2I.Zero, spriteImage.GetSize()), (Vector2I)(GetSpriteRect(sprite, towerToSave).Position - boundingBox.Position));
				}

				return image;
			}
			else
				return null;
		}
		else if (sprites.Count == 1)
			return sprites[0].Texture.GetImage();
		else
			return null;
	}

	public static Rect2 GetSpriteRect(Sprite2D sprite, Tower towerToSave)
	{
		Rect2 rect = new(towerToSave.ToLocal(sprite.GlobalPosition) + sprite.Offset, sprite.GetRect().Size);

		if (sprite.Centered)
			rect.Position -= rect.Size / 2f;

		return rect;
	}
}