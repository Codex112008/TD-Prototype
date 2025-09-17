using Godot;
using Godot.Collections;
using System;

public partial class TowerSelector : Selector
{
	public PackedScene SelectedTowerType;
	private Array<PackedScene> _towerTypeScenes;

	public override void OnItemSelected(int index)
	{
		SelectedTowerType = _towerTypeScenes[index];

		base.OnItemSelected(index);
	}

	public override void UpdateSelector()
	{
		_towerTypeScenes = TowerCreatorController.instance.TowerTypeScenes;
		foreach (PackedScene type in _towerTypeScenes)
		{
			Tower tempTower = type.Instantiate<Tower>();
			int index = ItemList.AddItem(Utils.SplitIntoPascalCase(type.ResourcePath[(type.ResourcePath.LastIndexOf('/') + 1)..type.ResourcePath.LastIndexOf(".tscn")]), ImageTexture.CreateFromImage(Utils.CreateImageFromSprites(tempTower)));
			ItemList.SetItemTooltip(index, tempTower.Tooltip);
			ItemList.SetItemTooltipEnabled(index, true);
			tempTower.QueueFree();
		}
	}

	public string SelectedTowerTypeName()
	{
		Tower tempTower = SelectedTowerType.Instantiate<Tower>();
		string tempTowerScriptName = tempTower.GetType().Name;
		tempTower.QueueFree();
		return tempTowerScriptName;
	}
}
