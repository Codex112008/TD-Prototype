using Godot;
using System;

public partial class Rand : Node
{
	// TODO: make unique randomnumbergenerators for stuff like enemy managers
	public static RandomNumberGenerator instance;
	public override void _Ready()
	{
		instance = new();
		instance.Randomize();
	}

	public static void SetFromSaveData(ulong savedSeed, ulong savedState)
	{
        instance = new()
        {
            Seed = savedSeed,
            State = savedState
        };
    }
}
