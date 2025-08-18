using Godot;
using Godot.Collections;
using System;

public partial class GameManager : Node
{
	public static GameManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one GameManager in scene!");
			return;
		}
		instance = this;
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
}
