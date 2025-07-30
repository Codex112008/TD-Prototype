using Godot;
using System;

public partial class StatSelector : HBoxContainer
{
	// TODO: basically finish this entire script, add dynamic cost display and do same for modifierSelector
	[Export] public RichTextLabel StatLabel;
	[Export] public RichTextLabel CostLabel;
	[Export] public SpinBox StatSpinBox;


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StatSpinBox.ValueChanged += TowerCreatorController.instance.UpdateTowerPreview;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
