using Godot;
using Godot.Collections;
using System;

[GlobalClass]
public abstract partial class Enemy : CharacterBody2D
{
	[Export] public Dictionary<EnemyStat, int> EnemyStats;
}

public enum EnemyStat
{
	MaxHealth,
	Speed,
	Damage,
	Defence
}