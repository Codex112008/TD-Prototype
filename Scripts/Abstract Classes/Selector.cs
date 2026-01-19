using Godot;

public abstract partial class Selector : VBoxContainer
{
	[Export] public RichTextLabel ItemLabel;
	[Export] public ItemList ItemList;
	[Export] public TextureButton SelectedItemTextureButton;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		if (ItemList.ItemCount == 1)
		{
			DisableSelector();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public virtual void OnItemSelected(int index)
	{
		ToggleVisibility();
		if (SelectedItemTextureButton != null)
		{
			SelectedItemTextureButton.TextureNormal = ItemList.GetItemIcon(index);
			SelectedItemTextureButton.TexturePressed = ItemList.GetItemIcon(index);
		}
	}

	public void OnItemSelectedSignal(int index)
	{
		OnItemSelected(index);

		TowerCreatorController.instance.UpdateTowerPreview();
	}

	public void ToggleVisibility()
	{
		ItemList.Visible = !ItemList.Visible;
	}

	public int GetIndexFromText(string text)
	{
		for (int i = 0; i < ItemList.ItemCount; i++)
		{
			if (Utils.RemoveWhitespaces(ItemList.GetItemText(i)) == Utils.RemoveWhitespaces(text))
				return i;
		}
		return -1;
	}

	public void DisableSelector()
	{
		SelectedItemTextureButton.MouseFilter = MouseFilterEnum.Ignore;
		SelectedItemTextureButton.FocusMode = FocusModeEnum.None;
	}

	public abstract void UpdateSelector();
}
