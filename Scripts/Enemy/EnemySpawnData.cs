using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EnemySpawnData : Resource
{
	[Export] public PackedScene EnemyScene;
	public int Weight = -1;
	[Export] public int BaseWeight = 1;
	[Export] public int MinWave = 0;
	[Export] public int MaxWave = int.MaxValue;
	[Export] public int QtyLow;
	[Export] public int QtyMean;
	[Export] public int QtyHigh;
	[Export] public int QtyMin = 1;
	[Export] public int QtyMax = int.MaxValue;
	[Export] public float BaseSpawnDelay;
	[Export] public Array<EnemySpawnData> PairingChoices = [];
	[Export] public bool ForcedOnIntroductionWave = true;
}