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

	[Export] private string _levelSaveFilePath;
	[Export] public PackedScene LevelScene;
	[Export] public PackedScene TowerCreationScene;
	[Export] private AnimationPlayer _cameraAnimPlayer;

	public Node CurrentScene = null;
	private Node _managerParent;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		_managerParent = GetChild(0);

		CurrentScene = LevelScene.Instantiate();
		AddChild(CurrentScene);

		InitManagers();
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
			if (CurrentScene.SceneFilePath == LevelScene.ResourcePath)
			{
				SaveLevel();
				GetChild(0).RemoveChild(BuildingManager.instance);
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
				// If modifying a tower set scene as basetower
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
				GetChild(0).AddChild(BuildingManager.instance);
				InitManagers();
				LoadLevel();
			}
			else
				InitManagers();

			CurrentScene.ProcessMode = ProcessModeEnum.Inherit;
		}
	}

	private void SaveLevel()
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
		Dictionary<string, Variant> waveData = new() { { "CurrentWave", EnemyManager.instance.CurrentWave } };
		if (EnemyManager.instance.EnemyParent.GetChildCount() > 0)
			waveData["CurrentWave"] = (int)waveData["CurrentWave"] - 1;
		saveFile.StoreLine(Json.Stringify(waveData));

		// Store random number generator data
		Dictionary<string, Array<string>> rngData = [];
		foreach ((Node node, RandomNumberGenerator rand) in RNGManager.instance.RandInstances)
			rngData.Add(node.GetPath(), [rand.Seed.ToString(), rand.State.ToString()]);
		
		saveFile.StoreLine(Json.Stringify(rngData));

		// Save data of all nodes that need to be saved
		ISavable[] nodesToSave = [.. GetTree().GetNodesInGroup("Persist").Where(node => node is ISavable).Cast<ISavable>()];
		foreach (ISavable nodeToSave in nodesToSave)
		{
			// Check the node is an instanced scene so it can be instanced again during load.
			if (string.IsNullOrEmpty((nodeToSave as Node2D).SceneFilePath))
			{
				GD.Print($"persistent node is not an instanced scene, skipped");
				continue;
			}

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
					Dictionary<string, Variant> waveData = (Dictionary<string, Variant>)json.Data;
					EnemyManager.instance.CurrentWave = (int)waveData["CurrentWave"];
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
	}

	private void InitManagers()
	{
		foreach (Node manager in _managerParent.GetChildren())
		{
			if (manager.HasMethod("Init"))
				manager.Call("Init");
		}
	}
}
