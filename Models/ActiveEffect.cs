namespace Dungnz.Models;

public class ActiveEffect
{
    public StatusEffect Effect { get; set; }
    public int RemainingTurns { get; set; }
    public ActiveEffect(StatusEffect effect, int duration) { Effect = effect; RemainingTurns = duration; }
    public bool IsDebuff => Effect is StatusEffect.Poison or StatusEffect.Bleed or StatusEffect.Stun or StatusEffect.Weakened;
    public bool IsBuff => !IsDebuff;
}
