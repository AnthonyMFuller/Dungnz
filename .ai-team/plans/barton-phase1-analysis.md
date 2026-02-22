# Phase 1 Combat — Barton Analysis

**Date:** 2025-01-20  
**Author:** Barton  
**Purpose:** Systems-side analysis for Phase 1 UI/UX combat improvements

---

## 1.1 — HP/MP Bars in ShowCombatStatus

**Current Location:** Display layer only  
**File:** `Display/ConsoleDisplayService.cs` — `ShowCombatStatus()`

**What Barton Needs to Change:** Nothing. This is purely a display-layer enhancement using existing `player.HP`, `player.MaxHP`, `player.MP`, `player.MaxMP` values.

**Dependency on Phase 0:** ✅ **YES** — Requires `RenderBar()` helper (#269) and signature change to `ShowCombatStatus` with effect parameters (#271).

**Status:** Hill owns this. Barton updates call sites once signature changes (see 1.2).

---

## 1.2 — Active Status Effects in ShowCombatStatus

**Current Location:**  
- **Call site:** `Engine/CombatEngine.cs:298` — `_display.ShowCombatStatus(player, enemy);`
- **Status data:** `StatusEffectManager` tracks active effects per entity

**What Barton Needs to Change:**  
Update all `ShowCombatStatus` call sites in `CombatEngine` to pass active effect collections:
```csharp
// Before:
_display.ShowCombatStatus(player, enemy);

// After:
_display.ShowCombatStatus(player, enemy, 
    _statusEffects.GetActiveEffects(player),
    _statusEffects.GetActiveEffects(enemy));
```

**Dependency on Phase 0:** ✅ **YES** — Requires new `ShowCombatStatus` signature (#271).

**Status:** Barton wiring blocked until Hill merges Phase 0.

---

## 1.3 — Elite/Enrage/Special Enemy Tags

**Current Location:**  
- **Call site:** `Engine/CombatEngine.cs` — start of combat loop, before `ShowCombatStatus`
- **Data available:** `enemy.IsElite` (bool), `enemy.Type` (for Vampire, Wraith detection), `DungeonBoss.IsEnraged` (bool)

**What Barton Needs to Change:**  
1. Call new `ShowCombatEntryFlags(enemy)` once at combat start
2. For enrage status: either add `isEnraged` bool to `ShowCombatStatus` params, or have display layer downcast `Enemy` to `DungeonBoss` when needed

**Dependency on Phase 0:** ✅ **YES** — Requires `ShowCombatEntryFlags()` method (#271).

**Status:** Barton call-site wiring blocked until Hill merges Phase 0.

**Design Decision Needed:** How to expose enrage status?
- **Option A:** Add `bool isEnraged` parameter to `ShowCombatStatus` — clean but more params
- **Option B:** Display layer checks `if (enemy is DungeonBoss boss && boss.IsEnraged)` — keeps params minimal

---

## 1.4 — Colorize Turn Log (Crits, Damage, Healing)

**Current Location:**  
- **File:** `Engine/CombatEngine.cs:382` — `ShowRecentTurns()` method
- **Data structure:** `List<CombatTurn> _turnLog` — records include `IsCrit`, `IsDodge`, `Damage`, `StatusApplied`

**What Barton Needs to Change:**  
Pre-colorize turn log strings before appending to `_turnLog`. Current code builds strings in `ShowRecentTurns()`; move colorization logic there.

Example:
```csharp
// In ShowRecentTurns():
if (turn.IsCrit)
    line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Bold}{ColorCodes.Yellow}CRIT{ColorCodes.Reset} {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";
else if (turn.IsDodge)
    line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.Gray}dodged{ColorCodes.Reset}";
else
    line = $"  {turn.Actor}: {turn.Action} → {ColorCodes.BrightRed}{turn.Damage}{ColorCodes.Reset} dmg";
```

**Dependency on Phase 0:** ❌ **NO** — Uses existing `ColorCodes` and `ShowMessage`. No new display methods needed.

**Status:** ✅ **CAN IMPLEMENT NOW** — Independent of Hill's Phase 0 work.

---

## 1.5 — Level-Up Choice with Current Values

**Current Location:**  
- **File:** `Engine/CombatEngine.cs:685` — `CheckLevelUp()` method
- **Lines 695-715:** Inline level-up menu using `ShowMessage`

**What Barton Needs to Change:**  
Replace inline menu with call to new `ShowLevelUpChoice(player)` display method. Remove manual menu rendering from CombatEngine.

```csharp
// Before:
_display.ShowMessage("=== LEVEL UP BONUS — Choose a trait: ===");
_display.ShowMessage("[1] +5 Max HP");
_display.ShowMessage("[2] +2 Attack");
_display.ShowMessage("[3] +2 Defense");

// After:
_display.ShowLevelUpChoice(player);
```

Input handling and stat application logic stays in CombatEngine (separation of concerns).

**Dependency on Phase 0:** ✅ **YES** — Requires `ShowLevelUpChoice(Player)` method (#271).

**Status:** Barton call-site wiring blocked until Hill merges Phase 0.

---

## 1.6 — XP Progress Bar

**Current Location:**  
- **Post-combat XP message:** `Engine/CombatEngine.cs` — after `player.XP += enemy.XPValue;`
- **Stats display:** Display layer only (`ShowPlayerStats`)

**What Barton Needs to Change:**  
1. **Post-combat message:** After awarding XP, emit progress fraction:
```csharp
var xpToNext = 100 * player.Level;
_display.ShowMessage($"You gained {enemy.XPValue} XP. (Total: {player.XP}/{xpToNext} to next level)");
```

2. **Stats display:** Nothing — Hill updates `ShowPlayerStats` to add bar rendering.

**Dependency on Phase 0:** ⚠️ **PARTIAL**  
- Post-combat message: ❌ **NO** — Uses existing `ShowMessage`, can implement now
- Stats display bar: ✅ **YES** — Requires `RenderBar()` helper (#269)

**Status:** Post-combat XP message ✅ **CAN IMPLEMENT NOW**. Stats bar is Hill's domain.

---

## 1.7 — Ability Confirmation Feedback

**Current Location:**  
- **File:** `Engine/CombatEngine.cs:405` — `HandleAbilityMenu()` method
- **Ability execution:** `Systems/AbilityManager.cs:139` — `UseAbility()` method

**What Barton Needs to Change:**  
After successful ability activation, emit confirmation message:
```csharp
// In CombatEngine.HandleAbilityMenu(), after UseAbility returns Success:
var ability = _abilities.GetAbility(abilityType);
_display.ShowMessage($"{ColorCodes.Bold}{ColorCodes.Yellow}[{ability.Name} activated — {ability.Description}]{ColorCodes.Reset}");
```

**Dependency on Phase 0:** ❌ **NO** — Uses existing `ShowMessage` and `ColorCodes`.

**Status:** ✅ **CAN IMPLEMENT NOW** — Independent of Hill's Phase 0 work.

---

## 1.8 — Status Effect Immunity Feedback

**Current Location:**  
- **File:** `Systems/StatusEffectManager.cs:30` — `Apply()` method
- **Line 33:** Early return on immunity: `if (target is Enemy enemy && enemy.IsImmuneToEffects) return;`

**What Barton Needs to Change:**  
Add feedback message before returning on immunity:
```csharp
if (target is Enemy enemy && enemy.IsImmuneToEffects)
{
    _display.ShowMessage($"{enemy.Name} is immune to status effects!");
    return;
}
```

**Prerequisite:** Confirm `StatusEffectManager` has `IDisplayService` injected. Currently injected in constructor (line 19).

**Dependency on Phase 0:** ❌ **NO** — Uses existing `ShowMessage`.

**Status:** ✅ **CAN IMPLEMENT NOW** — Independent of Hill's Phase 0 work.

---

## 1.9 — Achievement Mid-Combat Notification

**Current Location:**  
- **Event source:** `Systems/GameEvents.cs` — `OnAchievementUnlocked` event
- **Combat loop:** `Engine/CombatEngine.cs` — subscribe in constructor

**What Barton Needs to Change:**  
1. Subscribe to `GameEvents.OnAchievementUnlocked` in CombatEngine constructor
2. Queue notification to display at start of next turn (not mid-turn)
3. Emit using existing `ShowMessage` with Bold+Yellow formatting

```csharp
// In constructor:
if (_events != null)
{
    _events.OnAchievementUnlocked += (sender, e) =>
    {
        _pendingAchievement = $"🏆 Achievement Unlocked: {e.Achievement.Name}";
    };
}

// At start of turn display block (before ShowCombatStatus):
if (_pendingAchievement != null)
{
    _display.ShowMessage($"{ColorCodes.Bold}{ColorCodes.Yellow}{_pendingAchievement}{ColorCodes.Reset}");
    _pendingAchievement = null;
}
```

**Dependency on Phase 0:** ❌ **NO** — Uses existing `ShowMessage` and event system.

**Status:** ⚠️ **BLOCKED** — Requires `GameEvents.OnAchievementUnlocked` event which doesn't exist yet. Achievement system currently only evaluates at run completion, not mid-combat. This feature requires:
1. Add `OnAchievementUnlocked` event to `GameEvents`
2. Move achievement evaluation from run-end to incremental (e.g., after each combat)
3. Wire up achievement triggers

This is architectural work beyond combat systems scope. Needs Coulson to design event contract and Romanoff to wire tests.

---

## 1.10 — Combat Entry Visual Separator

**Current Location:**  
- **Call site:** `Engine/CombatEngine.cs:221` — `RunCombat()` method, start of combat loop

**What Barton Needs to Change:**  
Call new `ShowCombatStart(enemy)` once at combat entry, before combat loop begins:
```csharp
// At start of RunCombat():
_display.ShowCombatStart(enemy);
// Then existing combat loop...
```

**Dependency on Phase 0:** ✅ **YES** — Requires `ShowCombatStart(Enemy)` method (#271).

**Status:** Barton call-site wiring blocked until Hill merges Phase 0.

---

## Summary

### ✅ Implemented (No Phase 0 Dependency)
1. **1.4 — Colorize turn log** (CombatEngine.ShowRecentTurns) ✅
2. **1.6 — Post-combat XP message** (CombatEngine, after XP award) ✅
3. **1.7 — Ability confirmation** (CombatEngine.HandleAbilityMenu) ✅
4. **1.8 — Immunity feedback** (StatusEffectManager.Apply) ✅

### ⏸ Blocked Until Phase 0 Merged
1. **1.1 — HP/MP bars** (Hill owns display)
2. **1.2 — Status effects in header** (needs signature change)
3. **1.3 — Elite/enrage tags** (needs ShowCombatEntryFlags)
4. **1.5 — Level-up menu** (needs ShowLevelUpChoice)
5. **1.10 — Combat start banner** (needs ShowCombatStart)

### ⚠️ Blocked on Architecture
1. **1.9 — Achievement notifications** (needs GameEvents.OnAchievementUnlocked event + incremental evaluation)

---

## Recommended Action

✅ **COMPLETED:** Items 1.4, 1.6 (post-combat part), 1.7, 1.8 implemented on branch `squad/272-phase1-combat-prep`. These deliver immediate combat feel improvements without waiting for Hill's infrastructure work.
