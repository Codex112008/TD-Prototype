using Godot;
using Godot.Collections;
using System;

public interface ISavable
{
	public Dictionary<string, Variant> Save();

	public void Load(Dictionary<string, Variant> saveData);
}
