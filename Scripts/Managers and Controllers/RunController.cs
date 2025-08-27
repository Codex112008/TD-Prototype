using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class RunController : Node2D
{
	[Export] private string _levelSaveFilePath;
	[Export] private PackedScene _levelScene;
	[Export] private PackedScene _towerCreationScene;

	public int Seed;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        BuildingManager.instance.Init();
		PathfindingManager.instance.Init();
		EnemyManager.instance.Init();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{

	}

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is InputEventKey eventKey)
		{
			if (eventKey.Pressed && eventKey.Keycode == Key.S)
			{
				SaveLevel();
			}
		}
	}

	public void SaveLevel()
	{
        using FileAccess saveFile = FileAccess.Open(_levelSaveFilePath + "SavedLevel.txt", FileAccess.ModeFlags.Write);
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
        string tilemapJsonString = Json.Stringify(new Dictionary<string, Variant>() { { "TilemapData", tilemapData } });
        saveFile.StoreLine(tilemapJsonString);

        // Store current wave
        saveFile.StoreLine(Json.Stringify(new Dictionary<string, Variant>() { { "CurrentWave", EnemyManager.instance.CurrentWave } }));

        // Store random number generator data
        Dictionary<string, Variant> rngData = new()
        {
            { "Seed", Rand.instance.Seed },
            { "State", Rand.instance.State}
        };
        saveFile.StoreLine(Json.Stringify(rngData));

        // Save data of all nodes that need to be saved
        ISavable[] nodesToSave = [.. GetTree().GetNodesInGroup("Persist").Cast<ISavable>()];
        foreach (ISavable nodeToSave in nodesToSave)
        {
            // Check the node is an instanced scene so it can be instanced again during load.
            // if (string.IsNullOrEmpty((nodeToSave as Node2D).SceneFilePath))
            // {
            // 	GD.Print($"persistent node is not an instanced scene, skipped");
            // 	continue;
            // }

            Dictionary<string, Variant> saveData = nodeToSave.Save();

            string jsonString = Json.Stringify(saveData);

            saveFile.StoreLine(jsonString);
        }
    }

	public void LoadLevel()
	{
		if (!FileAccess.FileExists(_levelSaveFilePath + "SaveGame.save"))
		{
			return; // Error! We don't have a save to load.
		}

		// Delete nodes so we dont clone them (i think its redundant for my use case though)
		ISavable[] savableNodes = [.. GetTree().Root.GetChildren(true).Where(node => node is ISavable).Cast<ISavable>()];
		foreach (ISavable savable in savableNodes)
		{
			if (savable is Node node)
				node.QueueFree();
		}

		// Load the file line by line and process that dictionary to restore the object it represents.
		using FileAccess saveFile = FileAccess.Open("user://savegame.save", FileAccess.ModeFlags.Read);

		int counter = 0;
		while (saveFile.GetPosition() < saveFile.GetLength())
		{
			string jsonString = saveFile.GetLine();

			// Creates the helper class to interact with JSON.
			var json = new Json();
			var parseResult = json.Parse(jsonString);
			if (parseResult != Error.Ok)
			{
				GD.Print($"JSON Parse Error: {json.GetErrorMessage()} in {jsonString} at line {json.GetErrorLine()}");
				continue;
			}

			switch (counter)
			{
				case 0: // Load tilemap data

					break;
				case 1: // Load current wave
				
					break;
				case 2: // Load rng data

					break;
				default: // Load all ISaveables

					break;
			}

			counter++;
		}
	}
}
