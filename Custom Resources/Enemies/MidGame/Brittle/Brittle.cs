using Godot;
using System;

public partial class Brittle : Enemy
{
	[Export] private Sprite2D resistIconSprite;

    public override void AddStatusEffectStacks(StatusEffect status, float statusStacks, bool decay = false)
    {
        base.AddStatusEffectStacks(status, statusStacks, decay);

		if (status == StatusEffect.Reinforcement)
			SetStatusEffectValue(status, 0);
    }

    public override float TakeDamage(float amount, DamageType damageType, bool defenceBreak = false)
    {
		if (damageType == DamageType.Poison)
		{
			_sprite.Modulate = new(_sprite.Modulate, 0.8f);
			resistIconSprite.Visible = true;
			GetTree().CreateTimer(0.5f).Connect(Timer.SignalName.Timeout, Callable.From(SetIconInvisible));
			return 0f;
		}
		
        return base.TakeDamage(amount, damageType, defenceBreak);
    }

	private void SetIconInvisible()
	{
		_sprite.Modulate = new(_sprite.Modulate, 1);
		resistIconSprite.Visible = false;
	}
}
