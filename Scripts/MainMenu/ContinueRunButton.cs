using Godot;
using System;

public partial class ContinueRunButton : Button
{
	[Export] private NewRunButton _newRunButton;
	[Export] private Texture2D _rightArrow;

	[Export] private PackedScene _runControllerScene;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		string levelSaveFilePath = "RuntimeData/LevelSaveFiles/";
		levelSaveFilePath = Utils.AddCorrectDirectoryToPath(levelSaveFilePath);
		DirAccess dirAccessLevel = DirAccess.Open(levelSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(levelSaveFilePath))
            DirAccess.MakeDirRecursiveAbsolute(levelSaveFilePath);

		string runSaveFilePath = "RuntimeData/RunSaveFiles/";
		runSaveFilePath = Utils.AddCorrectDirectoryToPath(runSaveFilePath);
		DirAccess dirAccessRun = DirAccess.Open(runSaveFilePath);
		if (!DirAccess.DirExistsAbsolute(runSaveFilePath))
            DirAccess.MakeDirRecursiveAbsolute(runSaveFilePath);

		if (dirAccessLevel.GetFiles().Length == 0 && dirAccessRun.GetFiles().Length == 0)
		{
			Disabled = true;
			Icon = null;
			_newRunButton.Icon = _rightArrow;
		}
		else
		{
			Icon = _rightArrow;
			_newRunButton.Icon = null;
		}
	} 
	
	public void OnPressed()
	{
		PathfindingManager.instance = null;
		GetTree().ChangeSceneToFile(_runControllerScene.ResourcePath);
	}
}
