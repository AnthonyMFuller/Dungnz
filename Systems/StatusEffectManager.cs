namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;

/// <summary>
/// Tracks and processes active status effects (poison, bleed, stun, regen, etc.) for both
/// player and enemy combat entities. Handles effect application, per-turn tick processing,
/// and stat modifier calculations, while respecting immunity flags.
/// </summary>
public class StatusEffectManager
{
    private readonly IDisplayService _display;
    private readonly Dictionary<object, List<ActiveEffect>> _activeEffects = new();
    
    /// <summary>
    /// Initialises the manager with the display service used to emit combat messages.
    /// </summary>
    /// <param name="display">The display service for showing status-effect combat messages.</param>
    public StatusEffectManager(IDisplayService display) { _display = display; }
    
    /// <summary>
    /// Applies a status effect to the target for the specified number of turns.
    /// If the same effect is already active, the remaining duration is extended to whichever
    /// value is higher rather than stacking duplicate entries. Enemies with
    /// <see cref="Enemy.IsImmuneToEffects"/> set to <see langword="true"/> are silently skipped.
    /// </summary>
    /// <param name="target">The combat entity (Player or Enemy) to receive the effect.</param>
    /// <param name="effect">The status effect to apply.</param>
    /// <param name="duration">The number of turns the effect should remain active.</param>
    public void Apply(object target, StatusEffect effect, int duration)
    {
        // Stone Golem and other immune enemies cannot receive effects
        if (target is Enemy enemy && enemy.IsImmuneToEffects)
        {
            _display.ShowMessage($"{enemy.Name} is immune to status effects!");
            return;
        }

        // ChaosKnight is immune to Stun
        if (target is Enemy stunImmuneEnemy && stunImmuneEnemy.IsStunImmune && effect == StatusEffect.Stun)
        {
            _display.ShowMessage($"The {stunImmuneEnemy.Name} shrugs off the stun!");
            return;
        }

        // Sentinel 4-pc set bonus: player stun immunity
        if (target is Player p && p.IsStunImmune && effect == StatusEffect.Stun)
        {
            _display.ShowColoredMessage("[Sentinel] Stun resisted!", ColorCodes.BrightCyan);
            return;
        }

        if (!_activeEffects.ContainsKey(target)) _activeEffects[target] = new List<ActiveEffect>();
        var existing = _activeEffects[target].FirstOrDefault(e => e.Effect == effect);
        if (existing != null) existing.RemainingTurns = Math.Max(existing.RemainingTurns, duration);
        else
        {
            _activeEffects[target].Add(new ActiveEffect(effect, duration));
            if (effect == StatusEffect.Curse)
                _display.ShowCombatMessage($"[Cursed] {GetTargetName(target)}'s ATK and DEF reduced by 25%!");
        }
    }
    
    /// <summary>
    /// Processes all effects active on the target at the start of their turn: applies per-turn
    /// damage or healing, decrements remaining duration, and removes effects that have expired.
    /// </summary>
    /// <param name="target">The combat entity whose turn is beginning.</param>
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
                    else if (target is Enemy e) { e.HP = Math.Max(0, e.HP - 3); _display.ShowCombatMessage($"{GetTargetName(target)} takes 3 poison damage!"); } // Fix #613
                    break;
                case StatusEffect.Bleed:
                    if (target is Player p2) { p2.TakeDamage(5); _display.ShowCombatMessage($"{GetTargetName(target)} takes 5 bleed damage!"); }
                    else if (target is Enemy e2) { e2.HP = Math.Max(0, e2.HP - 5); _display.ShowCombatMessage($"{GetTargetName(target)} takes 5 bleed damage!"); } // Fix #613
                    break;
                case StatusEffect.Regen:
                    if (target is Player p3) { p3.Heal(4); _display.ShowCombatMessage($"{GetTargetName(target)} regenerates 4 HP!"); }
                    else if (target is Enemy e3) { e3.HP = Math.Min(e3.MaxHP, e3.HP + 4); _display.ShowCombatMessage($"{GetTargetName(target)} regenerates 4 HP!"); }
                    break;
                case StatusEffect.Stun:
                    // Stun message is emitted by CombatEngine before ProcessTurnStart is called.
                    break;
                case StatusEffect.Burn:
                    if (target is Player pb) { pb.TakeDamage(8); _display.ShowCombatMessage($"{GetTargetName(target)} takes 8 burn damage!"); }
                    else if (target is Enemy eb) { eb.HP = Math.Max(0, eb.HP - 8); _display.ShowCombatMessage($"{GetTargetName(target)} takes 8 burn damage!"); } // Fix #613
                    break;
                case StatusEffect.Freeze:
                    // Freeze message is emitted by CombatEngine before ProcessTurnStart is called.
                    break;
                case StatusEffect.Silence:
                case StatusEffect.Curse:
                    // No per-turn effect; Silence blocks abilities, Curse modifies stats via GetStatModifier.
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
    
    /// <summary>Returns all currently active effects on the given target, or an empty list if none exist.</summary>
    /// <param name="target">The combat entity to query.</param>
    /// <returns>A list of <see cref="ActiveEffect"/> instances currently applied to the target.</returns>
    public List<ActiveEffect> GetActiveEffects(object target) => _activeEffects.ContainsKey(target) ? _activeEffects[target] : new List<ActiveEffect>();

    /// <summary>
    /// Returns <see langword="true"/> if the specified effect is currently active on the target.
    /// </summary>
    /// <param name="target">The combat entity to check.</param>
    /// <param name="effect">The status effect to look for.</param>
    /// <returns><see langword="true"/> if the effect is active; otherwise <see langword="false"/>.</returns>
    public bool HasEffect(object target, StatusEffect effect) => _activeEffects.ContainsKey(target) && _activeEffects[target].Any(e => e.Effect == effect);
    
    /// <summary>
    /// Removes all debuff effects from the target, such as poison, bleed, stun, or weakness,
    /// and emits a removal message for each one cleared.
    /// </summary>
    /// <param name="target">The combat entity to cleanse of debuffs.</param>
    public void RemoveDebuffs(object target)
    {
        if (!_activeEffects.ContainsKey(target)) return;
        var debuffs = _activeEffects[target].Where(e => e.IsDebuff).ToList();
        foreach (var d in debuffs) { _activeEffects[target].Remove(d); _display.ShowMessage($"{GetTargetName(target)}'s {d.Effect} effect has been removed!"); }
    }
    
    /// <summary>
    /// Calculates the net modifier to a named stat (e.g. "Attack" or "Defense") contributed by
    /// active effects such as <see cref="StatusEffect.Weakened"/> or <see cref="StatusEffect.Fortified"/>.
    /// </summary>
    /// <param name="target">The combat entity whose modifiers should be summed.</param>
    /// <param name="stat">The name of the stat to calculate modifiers for ("Attack" or "Defense").</param>
    /// <returns>The combined integer modifier to add to the base stat value.</returns>
    public int GetStatModifier(object target, string stat)
    {
        if (!_activeEffects.ContainsKey(target)) return 0;
        int mod = 0;
        foreach (var e in _activeEffects[target])
        {
            if (stat == "Attack" && e.Effect == StatusEffect.Weakened) mod -= GetBaseAttack(target) / 2;
            else if (stat == "Attack" && e.Effect == StatusEffect.Slow) mod -= GetBaseAttack(target) / 4;
            else if (stat == "Attack" && e.Effect == StatusEffect.BattleCry) mod += GetBaseAttack(target) / 4;
            else if (stat == "Defense" && e.Effect == StatusEffect.Fortified) mod += GetBaseDefense(target) / 2;
            else if (stat == "Attack" && e.Effect == StatusEffect.Curse) mod -= GetBaseAttack(target) / 4;
            else if (stat == "Defense" && e.Effect == StatusEffect.Curse) mod -= GetBaseDefense(target) / 4;
        }
        return mod;
    }
    
    private string GetTargetName(object t) => t switch { Player p => p.Name, Enemy e => e.Name, _ => "Unknown" };
    private int GetBaseAttack(object t) => t switch { Player p => p.Attack, Enemy e => e.Attack, _ => 0 };
    private int GetBaseDefense(object t) => t switch { Player p => p.Defense, Enemy e => e.Defense, _ => 0 };

    /// <summary>
    /// Notifies the manager that physical damage was dealt to the target.
    /// If the target is Frozen, the Freeze is immediately cleared (broken by impact).
    /// </summary>
    /// <param name="target">The combat entity that received physical damage.</param>
    public void NotifyPhysicalDamage(object target)
    {
        if (!_activeEffects.ContainsKey(target)) return;
        var freeze = _activeEffects[target].FirstOrDefault(e => e.Effect == StatusEffect.Freeze);
        if (freeze != null)
        {
            _activeEffects[target].Remove(freeze);
            _display.ShowCombatMessage("[Freeze broken by impact!]");
        }
    }

    /// <summary>Removes all active effects from the target, typically called when combat ends or the entity dies.</summary>
    /// <param name="target">The combat entity whose effect list should be cleared.</param>
    public void Clear(object target) { if (_activeEffects.ContainsKey(target)) _activeEffects.Remove(target); }
}
