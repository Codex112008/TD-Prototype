using Godot;
using System;
using System.IO;

public partial class NewRunButton : Button
{
	[Export] private PackedScene _runControllerScene;

	public void OnPressed()
	{
		DeleteExistingSave();

		PathfindingManager.instance = null;
		GetTree().ChangeSceneToFile(_runControllerScene.ResourcePath);
	}

	public static void DeleteExistingSave()
	{
		string levelSaveFilePath = "RuntimeData/LevelSaveFiles/";
		levelSaveFilePath = Utils.AddCorrectDirectoryToPath(levelSaveFilePath);
		DirAccess dirAccess = DirAccess.Open(levelSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(levelSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(levelSaveFilePath);
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(file);

		string runSaveFilePath = "RuntimeData/RunSaveFiles/";
		runSaveFilePath = Utils.AddCorrectDirectoryToPath(runSaveFilePath);
		dirAccess.ChangeDir(runSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(runSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(runSaveFilePath);
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(file);

		string towerDataSaveFilePath = "RuntimeData/SavedTowers/";
		towerDataSaveFilePath = Utils.AddCorrectDirectoryToPath(towerDataSaveFilePath);
		dirAccess.ChangeDir(towerDataSaveFilePath);
		Utils.RemoveDirRecursive(towerDataSaveFilePath);
		DirAccess.MakeDirRecursiveAbsolute(towerDataSaveFilePath);
	}
}
