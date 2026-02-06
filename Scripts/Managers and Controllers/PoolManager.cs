using Godot;
using Godot.Collections;
using System;
using System.Linq;

public partial class PoolManager : Node2D, IManager
{
	public static PoolManager instance;
	public override void _EnterTree()
	{
		if (instance != null)
		{
			GD.PrintErr("More than one PoolManager in scene!");
			return;
		}
		instance = this;
	}

	[Export] private Dictionary<EnemySpawnData, int> _enemiesToPreload;
	[Export] private PackedScene _damageNumberScene;
	[Export] private int _damageNumbersToPreInstanciate = 1000;

	private int _preinstanciationPerFrame = 100;
	private Dictionary<string, Array<Enemy>> _enemyPool = [];
	private Array<DamageNumber> _damageNumberPool = [];
	private bool _startPreinstanciation = false;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_startPreinstanciation)
		{
			if (_enemiesToPreload.Keys.Count > 0)
			{
				EnemySpawnData enemyToInstanciate = _enemiesToPreload.Keys.First();
				int counter = 0;
				for (int i = 0; i < _preinstanciationPerFrame; i++)
				{
					counter++;
					if (_enemiesToPreload.TryGetValue(enemyToInstanciate, out int amount))
					{
						AddEnemyToPool(enemyToInstanciate.EnemyScene.Instantiate<Enemy>());

						_enemiesToPreload[enemyToInstanciate] = --amount;
						if (amount <= 0)
							_enemiesToPreload.Remove(enemyToInstanciate);
					}
					else
						break;
				}
				GD.Print(string.Concat("Added ", counter, " ", enemyToInstanciate.EnemyScene.ResourcePath[(enemyToInstanciate.EnemyScene.ResourcePath.LastIndexOf('/') + 1)..enemyToInstanciate.EnemyScene.ResourcePath.LastIndexOf('.')], " to pool!"));
			}
			else if (_damageNumbersToPreInstanciate > 0)
			{
				int counter = 0;
				for (int i = 0; i < _preinstanciationPerFrame; i++)
				{
					counter++;
					AddDamageNumberToPool(_damageNumberScene.Instantiate<DamageNumber>());
					_damageNumbersToPreInstanciate--;
					
					if (_damageNumbersToPreInstanciate <= 0)
						break;
				}
				GD.Print("Added " + counter + " damage number scenes too pool");
			}
			else
				_startPreinstanciation = false;

			if (Engine.GetFramesPerSecond() > 65)
				_preinstanciationPerFrame++;
			else if (Engine.GetFramesPerSecond() < 60)
				_preinstanciationPerFrame--;
		}
	}

	public void AddEnemyToPool(Enemy enemy)
	{
		enemy.GetParent()?.RemoveChild(enemy);

		// Reset status effects of enemy
		foreach (StatusEffect status in Enum.GetValues(typeof(StatusEffect)).Cast<StatusEffect>())
			enemy.SetStatusEffectValue(status, 0f);

		foreach (Timer timer in enemy.TimerEffectTimers)
			timer.Stop();
		
		// Disable enemy visibility and processing just in case
		enemy.Visible = false;
		enemy.ProcessMode = ProcessModeEnum.Disabled;
		if (_enemyPool.TryGetValue(enemy.SceneFilePath[(enemy.SceneFilePath.LastIndexOf('/') + 1)..enemy.SceneFilePath.LastIndexOf('.')], out Array<Enemy> enemiesInPool))
		{
			enemiesInPool.Add(enemy);
		}
		else
			_enemyPool.Add(enemy.SceneFilePath[(enemy.SceneFilePath.LastIndexOf('/') + 1)..enemy.SceneFilePath.LastIndexOf('.')], [enemy]);

		if (!_startPreinstanciation)
			GD.Print(string.Concat("Added ", enemy.SceneFilePath[(enemy.SceneFilePath.LastIndexOf('/') + 1)..enemy.SceneFilePath.LastIndexOf('.')], " to pool!"));
	}

	public bool TryPopEnemyFromPool(string enemyToGetName, out Enemy poppedEnemy)
	{
		if (_enemyPool.TryGetValue(enemyToGetName, out Array<Enemy> enemiesInPool) && enemiesInPool.Count > 0)
		{
			Enemy foundEnemy = enemiesInPool.Last();
			enemiesInPool.RemoveAt(enemiesInPool.Count - 1);
			foundEnemy.Visible = true;
			foundEnemy.ProcessMode = ProcessModeEnum.Inherit;
			poppedEnemy = foundEnemy;

			GD.Print("Popped " + enemyToGetName + "!");

			return true;
		}
        else
        {
            poppedEnemy = null;
            return false;
        }
    }

	public void AddDamageNumberToPool(DamageNumber damageNumber)
	{
		damageNumber.GetParent()?.RemoveChild(damageNumber);
		damageNumber.Visible = false;
		damageNumber.Modulate = Colors.White;
		damageNumber.GlobalPosition = Vector2.Zero;
    	damageNumber.Scale = Vector2.One * 0.25f;
		damageNumber.ProcessMode = ProcessModeEnum.Disabled;
		_damageNumberPool.Add(damageNumber);
	}

	public bool TryPopDamageNumberFromPool(out DamageNumber poppedDamageNumber)
	{
		if (_damageNumberPool.Count > 0)
		{
			DamageNumber foundDamageNumber = _damageNumberPool.Last();
			_damageNumberPool.RemoveAt(_damageNumberPool.Count - 1);
			foundDamageNumber.Visible = true;
			foundDamageNumber.ProcessMode = ProcessModeEnum.Inherit;
			foundDamageNumber.RequestReady();
			poppedDamageNumber = foundDamageNumber;
			
			return true;
		}
		else
		{
			poppedDamageNumber = null;
			return false;
		}
	}

    public void Init()
    {
        _startPreinstanciation = true;
    }

    public void Deload()
	{
		instance = null;
	}
}
