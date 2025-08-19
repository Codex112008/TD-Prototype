using Godot;
using Godot.Collections;

[GlobalClass]
public partial class EnemySpawnData : Resource
{
	[Export] public string Name;
	[Export] public PackedScene EnemyScene;
	public int Weight;
	[Export] public int BaseWeight;
	[Export] public int MinWave;
	[Export] public int MaxWave;
	[Export] public int QtyLow;
	[Export] public int QtyMean;
	[Export] public int QtyHigh;
	[Export] public int QtyMin;
	[Export] public int QtyMax;
	[Export] public Array<EnemySpawnData> PairingChoices;
}