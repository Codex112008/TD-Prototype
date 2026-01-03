using Godot;
using Godot.Collections;
using System;

public partial class TestingEnemySelectionUiPanel : PanelContainer
{
	[Export] public VBoxContainer enemySelectionButtonsContainer;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Array<EnemySpawnData> spawnData = EnemyManager.instance.EnemiesToSpawnData;
		for (int i = 0; i < spawnData.Count; i++)
		{
			Button button = new()
			{
				Text = Utils.SplitIntoPascalCase(spawnData[i].ResourcePath[(spawnData[i].ResourcePath.LastIndexOf("Enemies/") + 8)..spawnData[i].ResourcePath.LastIndexOf('/')])
			};
			int index = i;
			button.Connect(BaseButton.SignalName.Pressed, Callable.From(() => ChangeSelectedEnemy(index)));
			enemySelectionButtonsContainer.AddChild(button);
		}
	}

	public void ChangeSelectedEnemy(int index)
	{
		EnemyManager.instance.SelectedTestingEnemy = EnemyManager.instance.EnemiesToSpawnData[index];
	}

	public void ToggleVisibility()
	{
		enemySelectionButtonsContainer.Visible = !enemySelectionButtonsContainer.Visible;
	}
}
