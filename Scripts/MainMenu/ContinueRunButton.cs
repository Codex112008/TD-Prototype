using Godot;
using System;

public partial class ContinueRunButton : Button
{
	[Export] private PackedScene _runControllerScene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string levelSaveFilePath = "RuntimeData/LevelSaveFiles/";
		levelSaveFilePath = OS.HasFeature("editor") ? "res://" + levelSaveFilePath : "user://" + levelSaveFilePath;
		DirAccess dirAccessLevel = DirAccess.Open(levelSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(levelSaveFilePath))
            DirAccess.MakeDirRecursiveAbsolute(levelSaveFilePath);

		string runSaveFilePath = "RuntimeData/RunSaveFiles/";
		runSaveFilePath = OS.HasFeature("editor") ? "res://" + runSaveFilePath : "user://" + runSaveFilePath;
		DirAccess dirAccessRun = DirAccess.Open(runSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(runSaveFilePath))
            DirAccess.MakeDirRecursiveAbsolute(runSaveFilePath);

		if (dirAccessLevel.GetFiles().Length == 0 && dirAccessRun.GetFiles().Length == 0)
			Disabled = true;
	} 
	
	public void OnPressed()
	{
		GetTree().ChangeSceneToFile(_runControllerScene.ResourcePath);
	}
}
