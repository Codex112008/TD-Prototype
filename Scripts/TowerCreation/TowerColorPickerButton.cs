using Godot;
using System;

public partial class TowerColorPickerButton : ColorPickerButton
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void WhenPickerCreated()
	{
		ColorPicker colorPicker = GetPicker();
		colorPicker.EditAlpha = false;
		colorPicker.SamplerVisible = false;
		colorPicker.ColorModesVisible = false;
		colorPicker.PresetsVisible = false;

		Connect(ColorPickerButton.SignalName.ColorChanged, Callable.From((Color color) => TowerCreatorController.instance.UpdateTowerPreview()));
	}
}
