using Godot;
using Godot.Collections;
using System;

public partial class ModifierSelector : Selector
{
	[Export] public RichTextLabel CostLabel;
	[Export] public Array<TowerComponent> ModifiersToDisplay;
	public TowerComponent SelectedComponent;

	public override void OnItemSelected(int index)
	{
		SelectedComponent = ModifiersToDisplay[index];

		base.OnItemSelected(index);
	}
	
    public override void UpdateSelector()
	{
		foreach (TowerComponent modifer in ModifiersToDisplay)
		{
			int index = ItemList.AddItem(TowerCreatorController.SplitIntoPascalCase(modifer.ResourceName), modifer.Icon);
			ItemList.SetItemTooltip(index, modifer.Tooltip);
			ItemList.SetItemTooltipEnabled(index, true);
		}
	}
}
