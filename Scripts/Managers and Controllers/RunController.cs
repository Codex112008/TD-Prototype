using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class RunController : Node2D
{
	public static RunController instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one RunController in scene!");
			return;
		}
		instance = this;
	}

	[Export] private string _runSaveFilePath = "RuntimeData/RunSaveFiles/";
	[Export] private string _levelSaveFilePath = "RuntimeData/LevelSaveFiles/";
	[Export] public PackedScene LevelScene;
	[Export] public PackedScene TowerCreationScene;
	[Export] private AnimationPlayer _cameraAnimPlayer;
	[Export] private Node _managerParent;

	public Node CurrentScene = null;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_runSaveFilePath = OS.HasFeature("editor") ? "res://" + _runSaveFilePath : "user://" + _runSaveFilePath;
		if (!DirAccess.DirExistsAbsolute(_runSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(_runSaveFilePath);

		_levelSaveFilePath = OS.HasFeature("editor") ? "res://" + _levelSaveFilePath : "user://" + _levelSaveFilePath;
		if (!DirAccess.DirExistsAbsolute(_levelSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(_levelSaveFilePath);

		CurrentScene = LevelScene.Instantiate();
		AddChild(CurrentScene);

		InitManagers();

		DirAccess dirAccess = DirAccess.Open(_levelSaveFilePath);
		if (dirAccess.GetFiles().Length > 0)
			LoadLevel();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			if (eventKey.Pressed && eventKey.Keycode == Key.W)
			{
				SwapScene(TowerCreationScene, Key.W);
			}
			else if (eventKey.Pressed && eventKey.Keycode == Key.S)
			{
				SwapScene(LevelScene, Key.S);
			}
		}
	}

	public async void SwapScene(PackedScene scene, Key direction)
	{
		if (scene.ResourcePath != CurrentScene.SceneFilePath)
		{
			// Swapping FROM the level
			if (CurrentScene.SceneFilePath == LevelScene.ResourcePath)
			{
				SaveLevel();
				if (BuildingManager.instance.GetParent() == _managerParent)
					_managerParent.RemoveChild(BuildingManager.instance);
			}

			// Play animation
			switch (direction)
			{
				case Key.W:
					_cameraAnimPlayer.Queue("TransitionOutUp");
					break;
				case Key.S:
					_cameraAnimPlayer.Queue("TransitionOutDown");
					break;
			}

			SetProcessUnhandledKeyInput(false);

			Node[] savables = [.. GetTree().GetNodesInGroup("Persist").Where(node => node is ISavable)];
			foreach (Node savable in savables)
				savable.QueueFree();

			await ToSignal(_cameraAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);

			// Load the scene to swap to
			CurrentScene?.Free();
			CurrentScene = scene.Instantiate();
			CurrentScene.ProcessMode = ProcessModeEnum.Disabled;
			_managerParent.ProcessMode = ProcessModeEnum.Disabled;

			// If tower creator init it
			if (scene.ResourcePath == TowerCreationScene.ResourcePath)
			{
				(CurrentScene as TowerCreatorController).BaseTowerFileName = BuildingManager.instance.GetSelectedTowerFolderName();
			}

			// Play another animation
			switch (direction)
			{
				case Key.W:
					_cameraAnimPlayer.Queue("TransitionInUp");
					break;
				case Key.S:
					_cameraAnimPlayer.Queue("TransitionInDown");
					break;
			}

			AddChild(CurrentScene);

			await ToSignal(_cameraAnimPlayer, AnimationPlayer.SignalName.AnimationFinished);

			SetProcessUnhandledKeyInput(true);

			_managerParent.ProcessMode = ProcessModeEnum.Inherit;


			if (scene.ResourcePath == LevelScene.ResourcePath && FileAccess.FileExists(_levelSaveFilePath + "SavedLevel.save"))
			{
				_managerParent.AddChild(BuildingManager.instance);
				InitManagers();
				LoadLevel();
			}
			else
				InitManagers();

			CurrentScene.ProcessMode = ProcessModeEnum.Inherit;
		}
	}

	public void SaveLevel()
	{
		if (CurrentScene.SceneFilePath != LevelScene.ResourcePath)
		{
			GD.PrintErr("Current scene not the main level!");
			return;
		}

		using FileAccess saveFile = FileAccess.Open(_levelSaveFilePath + "SavedLevel.save", FileAccess.ModeFlags.Write);

		// Store tilemap level data
		TileMapLayer levelTilemap = PathfindingManager.instance.LevelTilemap;
		Array<Dictionary<string, Variant>> tilemapData = [];
		for (int i = 0; i < levelTilemap.GetUsedRect().Size.X; i++)
		{
			for (int j = 0; j < levelTilemap.GetUsedRect().Size.Y; j++)
			{
				Vector2I tilePos = new Vector2I(i, j) + levelTilemap.GetUsedRect().Position;
				Vector2I atlasCoords = levelTilemap.GetCellAtlasCoords(tilePos);
				Dictionary<string, Variant> tileData = new()
				{
					{"PosX", tilePos.X},
					{"PosY", tilePos.Y},
					{"SourceID", levelTilemap.GetCellSourceId(tilePos)},
					{"AtlasCoordX", atlasCoords.X},
					{"AtlasCoordY", atlasCoords.Y}
				};
				tilemapData.Add(tileData);
			}
		}
		saveFile.StoreLine(Json.Stringify(new Dictionary<string, Variant>() { { "TilemapData", tilemapData } }));

		// Store current wave
		Dictionary<string, Variant> gameData = new() {
			{ "CurrentWave", EnemyManager.instance.CurrentWave },
			{ "CurrentPlayerCurrency", BuildingManager.instance.PlayerCurrency }
		};
		if (EnemyManager.instance.EnemyParent.GetChildCount() > 0)
			gameData["CurrentWave"] = EnemyManager.instance.EnemyParent.GetChildren().Where(child => child is Enemy).Cast<Enemy>().OrderBy(child => child.SpawnedWave).ElementAt(0).SpawnedWave - 1;
		saveFile.StoreLine(Json.Stringify(gameData));

		// Store random number generator data
		Dictionary<string, Array<string>> rngData = [];
		foreach ((Node node, RandomNumberGenerator rand) in RNGManager.instance.RandInstances)
			rngData.Add(node.GetPath(), [rand.Seed.ToString(), rand.State.ToString()]);

		saveFile.StoreLine(Json.Stringify(rngData));

		// Save data of all nodes that need to be saved
		ISavable[] nodesToSave = [.. GetTree().GetNodesInGroup("Persist").OfType<ISavable>()];
		foreach (ISavable nodeToSave in nodesToSave)
		{
			// Check the node is an instanced scene so it can be instanced again during load.
			if (string.IsNullOrEmpty((nodeToSave as Node2D).SceneFilePath))
				continue;

			// Check if any towers are building previews and skip them
			if (nodeToSave is Tower tower && tower.IsBuildingPreview)
				continue;

			Dictionary<string, Variant> saveData = nodeToSave.Save();
			string jsonString = Json.Stringify(saveData);
			saveFile.StoreLine(jsonString);
		}
	}

	private void LoadLevel()
	{
		// Delete nodes so we dont clone them (i think its redundant for my use case though)
		ISavable[] savableNodes = [.. GetChildren(true).OfType<ISavable>()];
		foreach (ISavable savable in savableNodes)
		{
			if (savable is Node node)
				node.QueueFree();
		}

		// Load the file line by line and process that dictionary to restore the object it represents.
		using FileAccess saveFile = FileAccess.Open(_levelSaveFilePath + "SavedLevel.save", FileAccess.ModeFlags.Read);

		int counter = 0;
		while (saveFile.GetPosition() < saveFile.GetLength())
		{
			string jsonString = saveFile.GetLine();

			// Creates the helper class to interact with JSON.
			Json json = new();
			Error parseResult = json.Parse(jsonString);
			if (parseResult != Error.Ok)
			{
				GD.Print($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
				continue;
			}

			switch (counter)
			{
				case 0: // Load tilemap data
					Array<Dictionary<string, Variant>> tilemapData = (Array<Dictionary<string, Variant>>)((Dictionary<string, Variant>)json.Data)["TilemapData"];
					PathfindingManager.instance.LevelTilemap.Clear();
					foreach (Dictionary<string, Variant> tileData in tilemapData)
					{
						PathfindingManager.instance.LevelTilemap.SetCell(
							new Vector2I((int)tileData["PosX"], (int)tileData["PosY"]),
							(int)tileData["SourceID"],
							new Vector2I((int)tileData["AtlasCoordX"], (int)tileData["AtlasCoordY"])
						);
					}
					break;
				case 1: // Load current wave
					Dictionary<string, Variant> gameData = (Dictionary<string, Variant>)json.Data;
					EnemyManager.instance.CurrentWave = (int)gameData["CurrentWave"];
					BuildingManager.instance.PlayerCurrency = (int)gameData["CurrentPlayerCurrency"];
					break;
				case 2: // Load rng data
					Dictionary<string, Array<string>> rngData = (Dictionary<string, Array<string>>)json.Data;
					foreach ((string nodePath, Array<string> randData) in rngData)
						RNGManager.instance.SetFromSaveData(GetNode(nodePath), ulong.Parse(randData[0]), ulong.Parse(randData[1]));
					break;
				default: // Load all ISaveables
					Dictionary<string, Variant> nodeData = (Dictionary<string, Variant>)json.Data;
					PackedScene nodeScene = GD.Load<PackedScene>(nodeData["SceneFilePath"].ToString());
					ISavable instancedNode = nodeScene.Instantiate<ISavable>();
					instancedNode.Load(nodeData);
					GetNode(nodeData["Parent"].ToString()).AddChild((Node)instancedNode);
					break;
			}
			counter++;
		}

		InitManagers();
	}

	private void InitManagers()
	{
		foreach (Node node in _managerParent.GetChildren())
		{
			if (node is IManager manager)
				manager.Init();
		}
	}

	public void DeloadRun()
	{
		foreach (Node node in _managerParent.GetChildren())
		{
			if (node is IManager manager)
				manager.Deload();
		}
		instance = null;
	}
}
