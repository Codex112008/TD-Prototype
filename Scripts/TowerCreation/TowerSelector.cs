using Godot;
using Godot.Collections;
using System;

public partial class TowerSelector : Selector
{
	[Export] public Array<PackedScene> TowerTypesToDisplay;

    public override void OnItemSelected(int index)
	{


		base.OnItemSelected(index);
	}

	public override void UpdateSelector()
	{
		foreach (PackedScene type in TowerCreatorController.instance.TowerTypeScenes)
		{
			Tower tempTower = type.Instantiate<Tower>();
			int index = ItemList.AddItem(TowerCreatorController.SplitIntoPascalCase(type.ResourcePath[type.ResourcePath.LastIndexOf('/')..type.ResourcePath.LastIndexOf(".tscn")]), ImageTexture.CreateFromImage(TowerCreatorController.CreateImageFromSprites(tempTower)));
			ItemList.SetItemTooltip(index, tempTower.Tooltip);
			ItemList.SetItemTooltipEnabled(index, true);
			tempTower.QueueFree();
		}
	}
}
