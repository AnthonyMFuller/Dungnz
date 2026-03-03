# Deep Architecture Audit — Findings

**Date:** 2025-07-21  
**By:** Coulson  
**Scope:** Full codebase audit of Engine/, Models/, Systems/, Display/, Program.cs

## Summary

Audited all files across the four layers. Found 19 new issues not covered by existing filed issues (#931–#963). Most critical: boss loot scaling is broken, enemy HP can go negative allowing over-counted damage stats, several boss phase abilities don't track damage to RunStats, and the `SetBonusManager.ApplySetBonuses` silently discards computed stat bonuses.

## Findings

### P0 — Crash / Data Loss

None found beyond existing filed issues.

### P1 — Gameplay Bugs

**F-01: Boss loot RollDrop never receives `isBossRoom` or `dungeonFloor`**  
File: `Engine/CombatEngine.cs:1485`  
The `HandleLootAndXP` method calls `enemy.LootTable.RollDrop(enemy, player.Level, lootDropMultiplier: _difficulty.LootDropMultiplier)` without passing `isBossRoom: true` or the current `dungeonFloor`. This means:
- Bosses never get the guaranteed Legendary drop (isBossRoom path)
- Floor-scaled Epic/Legendary chances (floors 5–8) never fire
- Boss explicit drops (BossKey) still work via AddDrop, but the tiered loot system is completely bypassed for bosses
Suggested fix: Thread `dungeonFloor` through to CombatEngine (or via RunStats/CommandContext) and pass `isBossRoom: enemy is DungeonBoss` to `RollDrop`.

**F-02: Enemy HP can go negative — inflates DamageDealt stats**  
File: `Engine/CombatEngine.cs:805, 1278, 1286, 1429, 1446`  
Direct `enemy.HP -= playerDmg` can drive HP well below zero. The over-damage is added to `_stats.DamageDealt`, inflating the stat. Only periodic damage (line 347) clamps to `Math.Max(0, ...)`.
Suggested fix: Clamp all `enemy.HP -= X` to `Math.Max(0, ...)`, or cap the stat increment to the actual HP removed.

**F-03: Boss phase abilities don't track DamageTaken in RunStats**  
File: `Engine/CombatEngine.cs:1350–1422`  
`ExecuteBossPhaseAbility` deals damage via `player.TakeDamage(dmg)` for Reinforcements, TentacleBarrage, TidalSlam — but never increments `_stats.DamageTaken`. RunStats will undercount total damage on boss floors.
Suggested fix: Add `_stats.DamageTaken += dmg` after each `player.TakeDamage()` call in this method.

**F-04: SetBonusManager.ApplySetBonuses discards computed stat bonuses**  
File: `Systems/SetBonusManager.cs:180–186`  
The method computes `totalDef`, `totalHP`, `totalMana`, `totalDodge` from active set bonuses, then discards them with `_ = totalDef;` etc. The comment says "actual stat application is handled in CombatEngine / EquipmentManager as combat-time modifiers" — but CombatEngine only reads the 4-piece flag fields. The 2-piece stat bonuses (+10 MaxHP, +3 DEF, +20 MaxMana, +15% dodge) are never applied anywhere.
Suggested fix: Either apply these bonuses to the player in ApplySetBonuses, or have CombatEngine query GetActiveBonuses and apply the totals. Currently players get zero benefit from 2-piece set bonuses.

**F-05: Duplicate SoulHarvest heal — fires twice per kill for Necromancers**  
File: `Engine/CombatEngine.cs:817–821` and `Systems/SoulHarvestPassive.cs`  
CombatEngine has inline `player.Heal(5)` for Necromancer on enemy kill (line 817). SoulHarvestPassive does the same via GameEventBus subscription. However, GameEventBus is never wired in Program.cs and GameEvents doesn't publish OnEnemyKilled events. Currently only the inline version fires. If the bus is ever wired, Necromancers would heal 10 HP per kill instead of 5.
Suggested fix: Remove the inline heal and rely on the event-based system, or remove SoulHarvestPassive if the event bus isn't intended to be used.

### P2 — Tech Debt

**F-06: FinalFloor=8 duplicated in 4 places**  
Files: `Engine/GameLoop.cs:44`, `Engine/Commands/DescendCommandHandler.cs:8`, `Engine/Commands/GoCommandHandler.cs:9`, `Engine/Commands/StatsCommandHandler.cs:5`  
(Note: #959 covers the GameLoop copy. These 3 additional copies in command handlers are not covered.)
Suggested fix: Move to a single shared constant, e.g. `GameConstants.FinalFloor`.

**F-07: Hazard narration arrays duplicated between GameLoop and GoCommandHandler**  
Files: `Engine/GameLoop.cs:49–65`, `Engine/Commands/GoCommandHandler.cs:19–37`  
`_spikeHazardLines`, `_poisonHazardLines`, `_fireHazardLines` are identically duplicated. The GameLoop copies appear unused since hazard damage on room entry was moved to GoCommandHandler.
Suggested fix: Delete the unused copies in GameLoop, or extract to a shared narration class.

**F-08: Levenshtein distance duplicated across CommandParser and EquipmentManager**  
Files: `Engine/CommandParser.cs:217`, `Systems/EquipmentManager.cs:178`  
Two independent implementations. CommandParser's is private; EquipmentManager's is `internal static` and used by UseCommandHandler and TakeCommandHandler.
Suggested fix: Consolidate into a single utility method. CommandParser should call the EquipmentManager version.

**F-09: CombatEngine is 1,709 lines — classic god class**  
File: `Engine/CombatEngine.cs`  
`PerformPlayerAttack` alone is ~220 lines with deeply nested damage modifier chains. `PerformEnemyTurn` is ~460 lines. The class manages player attacks, enemy attacks, abilities, items, loot, XP, leveling, boss phases, status effects, narration, and passive effects.
Suggested fix: Extract `PlayerAttackResolver`, `EnemyTurnProcessor`, `LootDistributor`, and `BossPhaseHandler` as collaborators.

**F-10: UseCommandHandler is a 170-line `if`/`else if` chain for PassiveEffectId**  
File: `Engine/Commands/UseCommandHandler.cs:78–159`  
Each consumable with a PassiveEffectId is handled by a separate `else if` branch. Adding a new consumable effect requires modifying this handler.
Suggested fix: Extract a `ConsumableEffectRegistry` keyed by PassiveEffectId, with each effect as a delegate or strategy object.

**F-11: GameEventBus and GameEvents are parallel event systems — neither fully wired**  
Files: `Systems/GameEventBus.cs`, `Systems/GameEvents.cs`, `Systems/GameEventTypes.cs`  
GameEvents uses `event EventHandler<T>` pattern. GameEventBus uses `Subscribe<T>/Publish<T>`. Both exist, neither is fully used. GameEventBus is never instantiated in Program.cs. SoulHarvestPassive depends on GameEventBus but is never registered. OnEnemyKilled is defined but never published.
Suggested fix: Pick one event system, delete the other, and wire it properly. GameEventBus is the better design (decoupled pub/sub), but GameEvents is the one that's actually partially wired.

**F-12: StubCombatEngine left in production code**  
File: `Engine/StubCombatEngine.cs`  
Marked as "Temporary stub — replaced when Barton delivers CombatEngine" but still exists. It's `internal` so it won't leak, but it's dead code.
Suggested fix: Delete it.

### P3 — Code Smell / Design

**F-13: CommandContext carries 30+ fields and delegates — bag-of-everything anti-pattern**  
File: `Engine/Commands/CommandContext.cs`  
CommandContext has grown to include `HandleShrine`, `HandleContestedArmory`, `HandlePetrifiedLibrary`, `HandleTrapRoom` as delegates, plus `ExitRun`, `RecordRunEnd`, `GetCurrentlyEquippedForItem`, `GetDifficultyName`. This ties every command handler to GameLoop's implementation details.
Suggested fix: Extract shrine/armory/library/trap interactions into their own ICommandHandler implementations or a SpecialRoomHandler service.

**F-14: Player.Mana directly mutated in BloodDrain boss phase**  
File: `Engine/CombatEngine.cs:1383`  
`player.Mana = Math.Max(0, player.Mana - 10)` bypasses any future mana-change validation or events. All other mana changes go through `SpendMana()` or `RestoreMana()`.
Suggested fix: Add a `DrainMana(int amount)` method to Player, or use `SpendMana` with appropriate semantics.

**F-15: Necromancer MaxMana += 2 directly in HandleLootAndXP bypasses FortifyMaxMana**  
File: `Engine/CombatEngine.cs:1479–1480`  
`player.MaxMana += 2; player.Mana = Math.Min(player.Mana + 2, player.MaxMana)` — duplicates FortifyMaxMana logic without going through it.
Suggested fix: Use `player.FortifyMaxMana(2)` (though it requires amount > 0, which 2 satisfies).

**F-16: Ring of Haste passive check doesn't scan armor slots**  
File: `Engine/CombatEngine.cs:309–311`  
`if (player.EquippedAccessory?.PassiveEffectId == "cooldown_reduction" || player.EquippedWeapon?.PassiveEffectId == "cooldown_reduction")` — only checks weapon and accessory. If the cooldown_reduction passive were on an armor piece, it would be missed. PassiveEffectProcessor correctly scans all slots.
Suggested fix: Rely on PassiveEffectProcessor's OnCombatStart handling (which already fires), or scan `AllEquippedArmor` too.

**F-17: BossVariants constructor duplication — stat initialization repeated in parameterless and parameterized ctors**  
File: `Systems/Enemies/BossVariants.cs` (all boss classes)  
Every boss (GoblinWarchief, PlagueHoundAlpha, etc.) has two constructors that identically set Name, HP, MaxHP, Attack, Defense, XPValue, FloorNumber, and Phases. The parameterized constructor ignores the passed `stats` because it overrides everything with hardcoded values.
Suggested fix: Have the parameterized constructor call the parameterless one, or use a shared init method.

**F-18: AchievementSystem silently swallows all exceptions in LoadUnlocked/SaveUnlocked**  
File: `Systems/AchievementSystem.cs:117, 128`  
Bare `catch { }` blocks mean corrupted achievement data is silently ignored. If the JSON is malformed, achievements reset without warning.
Suggested fix: At minimum, log the exception via Trace like PrestigeSystem does.

**F-19: DescendCommandHandler doesn't pass `playerLevel` to DungeonGenerator.Generate**  
File: `Engine/Commands/DescendCommandHandler.cs:154`  
`gen.Generate(floorMultiplier: floorMult, difficulty: context.Difficulty, floor: context.CurrentFloor)` — the `playerLevel` parameter defaults to 1, so enemy scaling on lower floors ignores the player's actual level. This is partially offset by `floorMultiplier`, but enemies on floor 2 at player level 5 are scaled as if the player is level 1.
Suggested fix: Pass `playerLevel: context.Player.Level`.
