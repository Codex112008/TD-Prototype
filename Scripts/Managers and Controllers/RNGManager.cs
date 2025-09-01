using Godot;
using System;
using System.Collections.Generic;

public partial class RNGManager : Node
{
	public static RNGManager instance;
	public Dictionary<Node, RandomNumberGenerator> RandInstances = [];
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one RNGManager in scene!");
			return;
		}
		instance = this;

		RandInstances[this] = new();
		RandInstances[this].Randomize();
	}

	public override void _Ready()
	{
		
	}

	public void AddNewRNG(Node node)
	{
		RandInstances[node] = new();
		RandInstances[node].Seed = RandInstances[this].Randi();
	}

	public void SetFromSaveData(Node node, ulong savedSeed, ulong savedState)
	{
		RandInstances[node] = new()
		{
			Seed = savedSeed,
			State = savedState
		};
	}
}
