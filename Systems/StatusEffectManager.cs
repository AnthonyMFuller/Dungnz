namespace Dungnz.Systems;
using Dungnz.Models;
using Dungnz.Display;
public class StatusEffectManager
{
    private readonly IDisplayService _display;
    private readonly Dictionary<object, List<ActiveEffect>> _effects = new();
    public StatusEffectManager(IDisplayService display) => _display = display;
    public void Apply(object t, StatusEffect e, int d) { if (!_effects.ContainsKey(t)) _effects[t] = new(); _effects[t].Add(new ActiveEffect(e, d)); }
    public void ProcessTurnStart(object t) { if (!_effects.ContainsKey(t)) return; foreach (var e in _effects[t].ToList()) { e.RemainingTurns--; if (e.RemainingTurns <= 0) _effects[t].Remove(e); } }
    public List<ActiveEffect> GetActiveEffects(object t) => _effects.ContainsKey(t) ? _effects[t] : new();
    public bool HasEffect(object t, StatusEffect e) => _effects.ContainsKey(t) && _effects[t].Any(x => x.Effect == e);
    public void RemoveDebuffs(object t) { if (_effects.ContainsKey(t)) _effects[t].RemoveAll(e => e.IsDebuff); }
    public int GetStatModifier(object t, string s) => 0;
    public void Clear(object t) { if (_effects.ContainsKey(t)) _effects.Remove(t); }
}
