using Godot;

public abstract partial class Selector : VBoxContainer
{
	[Export] public RichTextLabel ItemLabel;
	[Export] public ItemList ItemList;
	[Export] private TextureButton _selectedItemButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void OnButtonPressed()
	{
		ItemList.Visible = !ItemList.Visible;
	}

	public virtual void OnItemSelected(int index)
	{
		ItemList.Visible = !ItemList.Visible;

		// TODO: Change _selectedModifierButton's texture to icon
		_selectedItemButton.TextureNormal = ItemList.GetItemIcon(index);
		_selectedItemButton.TexturePressed = ItemList.GetItemIcon(index);

		TowerCreatorController.instance.UpdateTowerPreview();
	}

	public int GetIndexFromText(string text)
	{
		for (int i = 0; i < ItemList.GetItemCount(); i++)
		{
			if (Utils.RemoveWhitespaces(ItemList.GetItemText(i)) == Utils.RemoveWhitespaces(text))
				return i;
		}
		return -1;
	}

	public abstract void UpdateSelector();
}
