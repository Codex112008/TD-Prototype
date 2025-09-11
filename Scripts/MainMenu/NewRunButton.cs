using Godot;
using System;
using System.IO;

public partial class NewRunButton : Button
{
	[Export] private PackedScene _runControllerScene;

	public void OnPressed()
	{
		string levelSaveFilePath = "RuntimeData/LevelSaveFiles/";
		levelSaveFilePath = OS.HasFeature("editor") ? "res://" + levelSaveFilePath : "user://" + levelSaveFilePath;
		DirAccess dirAccess = DirAccess.Open(levelSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(levelSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(levelSaveFilePath);
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(file);

		string runSaveFilePath = "RuntimeData/RunSaveFiles/";
		runSaveFilePath = OS.HasFeature("editor") ? "res://" + runSaveFilePath : "user://" + runSaveFilePath;
		dirAccess.ChangeDir(runSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(runSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(runSaveFilePath);
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(file);

		string towerDataSaveFilePath = "RuntimeData/SavedTowers/";
		towerDataSaveFilePath = OS.HasFeature("editor") ? "res://" + towerDataSaveFilePath : "user://" + towerDataSaveFilePath;
		dirAccess.ChangeDir(towerDataSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(towerDataSaveFilePath))
			DirAccess.MakeDirRecursiveAbsolute(towerDataSaveFilePath);
		RemoveRecursive(towerDataSaveFilePath);

		GetTree().ChangeSceneToFile(_runControllerScene.ResourcePath);
	}

	private void RemoveRecursive(string directory)
	{
		DirAccess dirAccess = DirAccess.Open(directory);
		foreach (string dir in dirAccess.GetDirectories())
			RemoveRecursive(directory.PathJoin(dir));
		foreach (string file in dirAccess.GetFiles())
			dirAccess.Remove(directory.PathJoin(file));
		dirAccess.Remove(directory);
	}
}
