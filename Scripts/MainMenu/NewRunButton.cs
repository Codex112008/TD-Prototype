using Godot;
using System;
using System.IO;

public partial class NewRunButton : Button
{
	[Export] private PackedScene _runControllerScene;

	public void OnPressed()
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
		if (!DirAccess.DirExistsAbsolute(towerDataSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(towerDataSaveFilePath);
		Utils.RemoveDirRecursive(towerDataSaveFilePath);

		GetTree().ChangeSceneToFile(_runControllerScene.ResourcePath);
	}
}
