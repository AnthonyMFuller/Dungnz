# Pre-v3 Bug Hunt Report
**Date:** 2026-02-20  
**Agent:** Barton (Systems Dev)  
**Scope:** Engine/CombatEngine.cs, Engine/EnemyFactory.cs, Engine/DungeonGenerator.cs, Systems/StatusEffectManager.cs, Systems/Enemies/*, Models/Enemy.cs  
**Requested by:** Copilot

## Executive Summary
Reviewed combat logic, status effect interactions, boss mechanics, enemy factory, and dungeon generation for bugs. **Identified 14 bugs** ranging from Critical (gameplay-breaking) to Low (polish/documentation). 

**Critical issues:** Status effect stat modifiers never applied (Fortified/Weakened do nothing), poison-on-hit mechanic inverted (player poisons themselves), half the enemy roster inaccessible in dungeons.

---

## Bug List

### 1. Status Effect Stat Modifiers Never Applied to Damage Calculations
**File:** `Engine/CombatEngine.cs` (lines 248, 294)  
**Severity:** **Critical**  
**Reproduction:**
1. Use Defensive Stance ability to apply Fortified (+50% DEF for 2 turns)
2. Enemy attacks player on next turn
3. Line 294 damage calculation: `var enemyDmg = Math.Max(1, enemy.Attack - player.Defense)`
4. Player takes full damage as if Fortified doesn't exist
5. Same issue with Weakened debuff on attack stat

**Root Cause:**  
`StatusEffectManager.GetStatModifier(target, "Attack"|"Defense")` is implemented and returns correct modifier values, but **is never called** in damage calculations. Fortified and Weakened effects apply, tick correctly, display messages, but have **zero gameplay impact**.

**Suggested Fix:**
```csharp
// Line 248 (player attacking enemy)
var playerDmg = Math.Max(1, 
    (player.Attack + _statusEffects.GetStatModifier(player, "Attack")) - 
    (enemy.Defense + _statusEffects.GetStatModifier(enemy, "Defense")));

// Line 294 (enemy attacking player)
var enemyDmg = Math.Max(1, 
    (enemy.Attack + _statusEffects.GetStatModifier(enemy, "Attack")) - 
    (player.Defense + _statusEffects.GetStatModifier(player, "Defense")));
```

**Impact:** Defensive Stance ability is useless. Weakened debuff (planned for Shrines) would have no effect. Core status effect system broken.

---

### 2. GoblinShaman Poison-on-Hit Mechanic Inverted
**File:** `Engine/CombatEngine.cs` (lines 259-260)  
**Severity:** **Critical**  
**Reproduction:**
1. Enter combat with Goblin Shaman (`AppliesPoisonOnHit = true`)
2. Player attacks Shaman (hits successfully)
3. Line 259: `if (enemy.AppliesPoisonOnHit && !enemy.IsImmuneToEffects)`
4. Line 260: `_statusEffects.Apply(player, StatusEffect.Poison, 3);`
5. **Player is poisoned by their own attack**

**Root Cause:**  
Poison-on-hit logic is in `PerformPlayerAttack()` method, not `PerformEnemyTurn()`. When player hits Shaman, player gets poisoned. Mechanic is completely backwards.

**Suggested Fix:**
```csharp
// REMOVE lines 259-260 from PerformPlayerAttack()

// ADD to PerformEnemyTurn() after line 311 (after damage message):
if (enemy.AppliesPoisonOnHit)
{
    _statusEffects.Apply(player, StatusEffect.Poison, 3);
    _display.ShowCombatMessage($"{enemy.Name} poisons you!");
}
```

**Impact:** GoblinShaman is currently **safer** to fight than normal goblins because attacking it poisons you (making you avoid attacking). Completely breaks enemy design intent.

---

### 3. DungeonGenerator Only Spawns 4 of 9 Enemy Types
**File:** `Engine/DungeonGenerator.cs` (line 114)  
**Severity:** **High**  
**Reproduction:**
1. Generate dungeon with `DungeonGenerator.Generate()`
2. Line 114: `var enemyTypes = new[] { "goblin", "skeleton", "troll", "darkknight" };`
3. Lines 123-127 spawn enemies from this list only
4. **GoblinShaman, StoneGolem, Wraith, VampireLord, Mimic never spawn**
5. Half the enemy roster is dead content

**Root Cause:**  
Enemy type list hardcoded to original 4 types. Never updated when 5 new enemies added in v2.

**Suggested Fix:**
```csharp
// Option 1: Add all 9 types to array
var enemyTypes = new[] { 
    "goblin", "skeleton", "troll", "darkknight",
    "goblinshaman", "stonegolem", "wraith", "vampirelord", "mimic" 
};

// Option 2 (better): Use EnemyFactory.CreateRandom() instead of manual list
if (_rng.NextDouble() < 0.6)
{
    room.Enemy = EnemyFactory.CreateRandom(_rng);
    // Then scale: room.Enemy = ScaleExistingEnemy(room.Enemy, playerLevel, floorMultiplier);
}
```

**Impact:** Players never encounter 5 enemy types. Balance testing incomplete. Mimic ambush mechanic untested in real gameplay.

---

### 4. Boss Enrage Check Allows Burst Damage to Skip Phase 2
**File:** `Engine/CombatEngine.cs` (lines 91-97)  
**Severity:** **Medium**  
**Reproduction:**
1. Boss at 82/200 HP (41% - above enrage threshold)
2. Player uses Power Strike + crit: 45 damage dealt
3. Boss drops to 37/200 HP (18.5% - well below 40%)
4. Line 99: `if (enemy.HP <= 0)` check fails (boss still alive)
5. Boss attacks player normally (not enraged)
6. Next turn: Line 94 `CheckEnrage()` triggers, boss enrages
7. If player kills boss this turn, boss **never enters Phase 2**

**Root Cause:**  
`CheckEnrage()` called at start of turn, not immediately after damage dealt. Allows boss HP to drop below 40% without triggering enrage until next turn.

**Suggested Fix:**
```csharp
// REMOVE lines 91-97 from main combat loop

// ADD to PerformPlayerAttack() after line 255 (enemy.HP -= playerDmg):
if (enemy is DungeonBoss boss)
{
    var wasEnraged = boss.IsEnraged;
    boss.CheckEnrage();
    if (!wasEnraged && boss.IsEnraged)
        _display.ShowCombatMessage("⚠ The boss ENRAGES! Its attack has increased by 50%!");
}
```

**Impact:** High-damage builds can skip boss Phase 2 entirely. Reduces difficulty and makes enrage mechanic unreliable.

---

### 5. Boss Telegraphed Charge Gives Player Free Turn
**File:** `Engine/CombatEngine.cs` (lines 281-286)  
**Severity:** **Medium**  
**Reproduction:**
1. Boss rolls 30% charge chance (line 280)
2. Line 283: `IsCharging = true`, displays "⚠ Boss is charging!"
3. Line 285: `return` — boss does **not** attack this turn
4. Main loop continues: player abilities tick, mana regenerates +10, status effects process (lines 87-88)
5. Player can freely use Second Wind (heal 30% HP), Defensive Stance, consume potions
6. Next turn: Boss unleashes 3x damage charge (lines 297-301)

**Root Cause:**  
Telegraph turn is a **free turn** for player to react. Boss pays full turn cost (no damage dealt) to warn player.

**Design Question:**  
Is this intentional? If telegraph meant to give counterplay window, current design works. If telegraph should be threatening, boss is currently **weaker** than normal attacks.

**Suggested Fix (if telegraph should be threatening):**
```csharp
// Option 1: Boss attacks normally AND telegraphs charge (remove line 285 return)
if (_rng.Next(100) < 30)
{
    boss.IsCharging = true;
    _display.ShowCombatMessage($"⚠ {enemy.Name} is charging a powerful attack! Prepare to defend!");
    // REMOVE: return;  // Let boss attack normally this turn
}

// Option 2: Skip player status/mana processing on telegraph turn
// (Move telegraph check BEFORE lines 84-88)
```

**Impact:** Boss encounter is easier than intended if telegraph should be threatening. If intentional, this is not a bug.

---

### 6. Boss ChargeActive Flag Sticks if Player Dodges Charged Attack
**File:** `Engine/CombatEngine.cs` (lines 296-302)  
**Severity:** **Medium**  
**Reproduction:**
1. Boss charges (IsCharging = true)
2. Next turn: Line 278 sets `ChargeActive = true`, line 277 clears `IsCharging`
3. Line 288: Player dodges attack (`RollDodge(player.Defense)` succeeds)
4. Line 290 shows "You dodged the attack!"
5. Charge damage logic (lines 297-301) only executes **inside** the `else` block (dodge failed)
6. `ChargeActive` never reset to `false`
7. **All subsequent boss attacks deal 3x damage** until charge finally lands

**Root Cause:**  
`ChargeActive = false` on line 299 is inside the dodge-failure branch. If player dodges, flag persists.

**Suggested Fix:**
```csharp
// Move ChargeActive reset OUTSIDE dodge check
if (enemy is DungeonBoss chargeBoss && chargeBoss.ChargeActive)
{
    chargeBoss.ChargeActive = false;  // Reset FIRST
    if (!RollDodge(player.Defense))   // Then check dodge
    {
        enemyDmg *= 3;
        _display.ShowCombatMessage($"⚡ {enemy.Name} unleashes the charged attack!");
    }
    else
    {
        _display.ShowCombatMessage("You dodged the charged attack!");
    }
}
```

**Impact:** Boss becomes extremely lethal if first charged attack is dodged. All future attacks deal 3x damage.

---

### 7. Mimic Ambush Bypasses Turn Start Processing
**File:** `Engine/CombatEngine.cs` (lines 74-80)  
**Severity:** **Medium**  
**Reproduction:**
1. Enter combat with Mimic (`IsAmbush = true`)
2. Lines 74-80 execute **before** main combat loop starts
3. `PerformEnemyTurn()` called immediately (Mimic strikes first)
4. Main loop begins at line 82
5. Lines 84-88: Status effects process, mana regenerates, cooldowns tick
6. **This happens on turn 2, not turn 1**
7. If player had Poison from previous fight, it doesn't tick during ambush turn

**Root Cause:**  
Ambush logic executes before turn structure established. Inconsistent with combat timing.

**Suggested Fix:**
```csharp
// Move ambush INSIDE main loop, after turn processing
bool ambushExecuted = false;
while (true)
{
    _statusEffects.ProcessTurnStart(player);
    _statusEffects.ProcessTurnStart(enemy);
    player.RestoreMana(10);
    _abilities.TickCooldowns();
    
    if (enemy.IsAmbush && !ambushExecuted)
    {
        _display.ShowCombatMessage($"It's a {enemy.Name}! You've been ambushed!");
        PerformEnemyTurn(player, enemy);
        ambushExecuted = true;
        if (player.HP <= 0) return CombatResult.PlayerDied;
        continue;  // Skip player turn, proceed to next loop iteration
    }
    
    // ... rest of combat loop
}
```

**Impact:** Ambush mechanic inconsistent with turn timing. Edge case bug if player enters Mimic fight with status effects active.

---

### 8. StatusEffectManager Double-Handles Stun Logic with CombatEngine
**File:** `Systems/StatusEffectManager.cs` (lines 67-69)  
**Severity:** **High**  
**Reproduction:**
1. Entity stunned (3 turn duration)
2. Turn starts, `ProcessTurnStart()` called (line 47)
3. Line 68 shows "cannot act!" message
4. Line 71 decrements duration
5. `ProcessTurnStart()` returns to CombatEngine
6. CombatEngine lines 108-114 (player) / 266-269 (enemy) check `HasEffect(Stun)` **again**
7. Combat loop skips turn a second time
8. If CombatEngine check removed, stun does nothing (ProcessTurnStart just shows message)

**Root Cause:**  
Stun logic split between StatusEffectManager (displays message) and CombatEngine (enforces skip). Fragile coupling.

**Suggested Fix:**
```csharp
// REMOVE lines 67-69 from StatusEffectManager.ProcessTurnStart()
// Stun should NOT show message or modify behavior in ProcessTurnStart
// CombatEngine owns the skip logic and should display the message

// StatusEffectManager only tracks duration:
case StatusEffect.Stun:
    // No message, no action
    break;

// CombatEngine already correctly handles stun (lines 108-114, 266-269)
```

**Impact:** Code duplication and unclear responsibility. If either side changes, stun breaks.

---

### 9. Boss Enrage Multiplier Compounds if Boss Heals Above 40%
**File:** `Systems/Enemies/DungeonBoss.cs` (line 98)  
**Severity:** **High** (currently dormant, but future-breaking)  
**Reproduction:**
1. Boss at 100 HP (50%), base attack 22
2. Boss drops to 40 HP (40%), enrages: `Attack = (int)(Attack * 1.5)` → Attack = 33
3. If boss heals to 50 HP (via Vampire lifesteal or future Regen mechanics), `IsEnraged` stays `true`
4. Boss takes damage to 39 HP again
5. Line 95: `if (!IsEnraged && HP <= MaxHP * 0.4)` — check **fails** (already enraged), exits
6. **However**, if status effects cleared mid-combat (e.g., debug command), `IsEnraged` resets to `false`
7. Boss drops below 40% again
8. Line 98 re-applies 1.5x to **current** Attack: `Attack = (int)(33 * 1.5)` = 49 (2.25x base)

**Root Cause:**  
Line 98 multiplies current Attack value, not stored base. If enrage triggers multiple times, multiplier compounds.

**Suggested Fix:**
```csharp
// Line 98 (DungeonBoss.cs)
Attack = (int)(_baseAttack * 1.5);  // Always calculate from stored base
```

**Impact:** Currently protected by "boss cannot heal" and "IsEnraged persists" logic, but **will break** if future mechanics allow boss healing or status clearing. Ticking time bomb.

---

### 10. Elite Variant Multiplier Applied After Config Stats Loaded
**File:** `Engine/EnemyFactory.cs` (lines 67-71)  
**Severity:** **Medium**  
**Reproduction:**
1. `EnemyFactory.Initialize()` loads config: Goblin has 20 HP, 8 ATK, 2 DEF
2. `CreateRandom()` rolls elite (5% chance)
3. Lines 67-69 apply 1.5x multiplier to **config stats**: `enemy.MaxHP = (int)(20 * 1.5)` = 30 HP
4. **Not currently exploitable** because `CreateRandom()` doesn't call `CreateScaled()`
5. But if caller chains `CreateRandom()` then manually scales enemy, multiplier stacks
6. OR if `CreateScaled()` adds elite flag parameter in future, double-scaling occurs

**Root Cause:**  
Elite logic happens in `CreateRandom()` after enemy construction. If scaling also applied, multipliers stack.

**Suggested Fix:**
```csharp
// Option 1: Pass isElite flag to CreateScaled()
public static Enemy CreateScaled(string enemyType, int playerLevel, float floorMultiplier = 1.0f, bool isElite = false)
{
    var scalar = (1.0f + (playerLevel - 1) * 0.12f) * floorMultiplier;
    if (isElite) scalar *= 1.5f;  // Integrate elite into scalar
    // ... rest of method
}

// Option 2: Remove elite logic from CreateRandom(), handle in caller
// DungeonGenerator decides if elite, passes flag to CreateScaled()
```

**Impact:** Fragile API design. Not currently exploitable but dangerous for future refactors.

---

### 11. Poison-on-Hit Immunity Check Tests Wrong Entity
**File:** `Engine/CombatEngine.cs` (line 259)  
**Severity:** **Low** (symptom of Bug #2)  
**Reproduction:**
1. Line 259: `if (enemy.AppliesPoisonOnHit && !enemy.IsImmuneToEffects)`
2. Checks if **enemy** is immune to effects
3. But line 260 applies poison to **player**
4. Should check if player is immune (though Player model has no `IsImmuneToEffects` property)

**Root Cause:**  
Symptom of Bug #2 (poison-on-hit inverted). Logic checks wrong entity's immunity.

**Suggested Fix:**  
Becomes moot once Bug #2 fixed. Correct implementation:
```csharp
// After enemy hits player (in PerformEnemyTurn):
if (enemy.AppliesPoisonOnHit)
{
    _statusEffects.Apply(player, StatusEffect.Poison, 3);
    // StatusEffectManager.Apply() already handles immunity checks internally
}
```

**Impact:** Minor. Player cannot be immune currently, so check is redundant anyway.

---

### 12. Critical Hit Chance Mismatch Between Code and Documentation
**File:** `Engine/CombatEngine.cs` (line 366)  
**Severity:** **Low**  
**Reproduction:**
1. Line 366: `return _rng.NextDouble() < 0.15;` (15% crit chance)
2. History.md line 219: "crits 20%, dodge DEF-based"
3. v3 planning docs reference 20% crit rate
4. Code implements 15%, docs say 20%

**Root Cause:**  
Unclear if 15% is intentional balance change or documentation error.

**Suggested Fix:**
```csharp
// If 20% is correct design:
return _rng.NextDouble() < 0.20;

// OR update all documentation to reflect 15% if intentional
```

**Impact:** Minor balance discrepancy. Affects player expectations vs. actual gameplay.

---

### 13. DungeonGenerator PathExists() Check is Dead Code
**File:** `Engine/DungeonGenerator.cs` (lines 156-160)  
**Severity:** **Low**  
**Reproduction:**
1. `Generate()` creates full W×H rectangular grid (lines 68-77)
2. Lines 80-102 connect all adjacent rooms bidirectionally
3. Full grid structure **guarantees** all rooms connected
4. Line 157: `PathExists(startRoom, exitRoom)` always returns `true`
5. Lines 158-160 comment acknowledges "should never happen" but leaves dead code

**Root Cause:**  
Safety check for partial/sparse grids, but generator always creates full grids.

**Suggested Fix:**
```csharp
// Option 1: Remove dead code
// DELETE lines 156-160

// Option 2: Implement actual corridor-adding logic for future partial grids
if (!PathExists(startRoom, exitRoom))
{
    ForceCorridorPath(startRoom, exitRoom);
}
```

**Impact:** Code smell, not functional bug. Becomes critical if generator evolves to create partial grids with missing exits.

---

### 14. DungeonGenerator Creates Only Rectangular Grids (Design Limitation)
**File:** `Engine/DungeonGenerator.cs` (lines 80-102)  
**Severity:** **Low** (not a bug, but design limitation)  
**Reproduction:**
1. Lines 80-102 create full W×H grid with all N/S/E/W connections
2. Every room has maximum exits (4 for interior, 2-3 for edges)
3. All dungeons feel identical
4. No dead ends, loops, branching paths, or layout variety

**Root Cause:**  
Full grid generation algorithm. No random exit removal.

**Suggested Enhancement (for v3):**
```csharp
// After grid creation, randomly remove 20-30% of exits:
foreach (var room in allRooms)
{
    if (_rng.NextDouble() < 0.25 && room != startRoom && room != exitRoom)
    {
        RemoveRandomExit(room);  // Remove 1 random exit
    }
}

// Then verify connectivity:
if (!PathExists(startRoom, exitRoom))
{
    // Restore removed exits until path exists
}
```

**Impact:** Dungeon layout variety limited. All floors feel samey. Not a bug per se, flagged for v3 planning.

---

## Summary Statistics
- **Total Bugs Found:** 14
- **Critical:** 2 (stat modifiers broken, poison-on-hit inverted)
- **High:** 3 (enemy roster inaccessible, stun double-handling, boss enrage compounding)
- **Medium:** 6 (boss mechanics issues, ambush timing, elite stacking)
- **Low:** 3 (documentation mismatches, dead code)

## Recommended Fix Priority
1. **Bug #1** (stat modifiers) — Blocks status effect system entirely
2. **Bug #2** (poison-on-hit) — Breaks GoblinShaman design
3. **Bug #3** (enemy spawning) — Half the content inaccessible
4. **Bug #8** (stun double-handling) — Fragile coupling
5. **Bug #6** (charge flag sticking) — Boss becomes unkillable
6. **Bug #4** (boss enrage timing) — Allows Phase 2 skip
7. **Bug #10** (elite stacking) — API design fragility
8. **Bug #9** (boss enrage compounding) — Future-breaking
9. Remaining bugs — Polish/documentation

## Testing Recommendations
After fixes:
1. Test all 6 status effects with `GetStatModifier()` integration
2. Fight GoblinShaman and verify poison applies when Shaman hits player
3. Verify all 9 enemy types spawn in dungeons
4. Test boss enrage triggers immediately at 40% HP
5. Test boss charge sequence: telegraph → charge → reset cycle
6. Test Mimic ambush with pre-existing status effects
7. Test elite variants spawn correctly via both CreateRandom() and CreateScaled()

---

**End of Report**
