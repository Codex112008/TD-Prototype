using Godot;
using System;

public partial class TowerColorPickerButton : ColorPickerButton
{
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
