namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

public class StatusEffectManager
{
    private readonly IDisplayService _display;
    private readonly Dictionary<object, List<ActiveEffect>> _activeEffects = new();
    
    public StatusEffectManager(IDisplayService display) { _display = display; }
    
    public void Apply(object target, StatusEffect effect, int duration)
    {
        // Stone Golem and other immune enemies cannot receive effects
        if (target is Enemy enemy && enemy.IsImmuneToEffects) return;

        if (!_activeEffects.ContainsKey(target)) _activeEffects[target] = new List<ActiveEffect>();
        var existing = _activeEffects[target].FirstOrDefault(e => e.Effect == effect);
        if (existing != null) existing.RemainingTurns = Math.Max(existing.RemainingTurns, duration);
        else _activeEffects[target].Add(new ActiveEffect(effect, duration));
    }
    
    public void ProcessTurnStart(object target)
    {
        if (!_activeEffects.ContainsKey(target)) return;
        var effects = _activeEffects[target];
        var toRemove = new List<ActiveEffect>();
        foreach (var ae in effects)
        {
            switch (ae.Effect)
            {
                case StatusEffect.Poison:
                    if (target is Player p) { p.TakeDamage(3); _display.ShowCombatMessage($"{GetTargetName(target)} takes 3 poison damage!"); }
                    else if (target is Enemy e) { e.HP -= 3; _display.ShowCombatMessage($"{GetTargetName(target)} takes 3 poison damage!"); }
                    break;
                case StatusEffect.Bleed:
                    if (target is Player p2) { p2.TakeDamage(5); _display.ShowCombatMessage($"{GetTargetName(target)} takes 5 bleed damage!"); }
                    else if (target is Enemy e2) { e2.HP -= 5; _display.ShowCombatMessage($"{GetTargetName(target)} takes 5 bleed damage!"); }
                    break;
                case StatusEffect.Regen:
                    if (target is Player p3) { p3.Heal(4); _display.ShowCombatMessage($"{GetTargetName(target)} regenerates 4 HP!"); }
                    else if (target is Enemy e3) { e3.HP = Math.Min(e3.MaxHP, e3.HP + 4); _display.ShowCombatMessage($"{GetTargetName(target)} regenerates 4 HP!"); }
                    break;
                case StatusEffect.Stun:
                    _display.ShowCombatMessage($"{GetTargetName(target)} is stunned and cannot act!");
                    break;
            }
            ae.RemainingTurns--;
            if (ae.RemainingTurns <= 0)
            {
                toRemove.Add(ae);
                _display.ShowCombatMessage($"{GetTargetName(target)}'s {ae.Effect} effect has worn off.");
            }
        }
        foreach (var effect in toRemove) effects.Remove(effect);
    }
    
    public List<ActiveEffect> GetActiveEffects(object target) => _activeEffects.ContainsKey(target) ? _activeEffects[target] : new List<ActiveEffect>();
    public bool HasEffect(object target, StatusEffect effect) => _activeEffects.ContainsKey(target) && _activeEffects[target].Any(e => e.Effect == effect);
    
    public void RemoveDebuffs(object target)
    {
        if (!_activeEffects.ContainsKey(target)) return;
        var debuffs = _activeEffects[target].Where(e => e.IsDebuff).ToList();
        foreach (var d in debuffs) { _activeEffects[target].Remove(d); _display.ShowMessage($"{GetTargetName(target)}'s {d.Effect} effect has been removed!"); }
    }
    
    public int GetStatModifier(object target, string stat)
    {
        if (!_activeEffects.ContainsKey(target)) return 0;
        int mod = 0;
        foreach (var e in _activeEffects[target])
        {
            if (stat == "Attack" && e.Effect == StatusEffect.Weakened) mod -= GetBaseAttack(target) / 2;
            else if (stat == "Defense" && e.Effect == StatusEffect.Fortified) mod += GetBaseDefense(target) / 2;
        }
        return mod;
    }
    
    private string GetTargetName(object t) => t switch { Player p => p.Name, Enemy e => e.Name, _ => "Unknown" };
    private int GetBaseAttack(object t) => t switch { Player p => p.Attack, Enemy e => e.Attack, _ => 0 };
    private int GetBaseDefense(object t) => t switch { Player p => p.Defense, Enemy e => e.Defense, _ => 0 };
    public void Clear(object target) { if (_activeEffects.ContainsKey(target)) _activeEffects.Remove(target); }
}
