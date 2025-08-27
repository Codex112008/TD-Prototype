using Godot;
using System;

public partial class Rand : Node
{
	public static RandomNumberGenerator instance;
	public override void _Ready()
	{
		instance = new();
		instance.Randomize();
	}

	public void SetFromSaveData(int savedSeed, int savedState)
	{
        instance = new()
        {
            Seed = (ulong)savedSeed,
            State = (ulong)savedState
        };
    }
}
