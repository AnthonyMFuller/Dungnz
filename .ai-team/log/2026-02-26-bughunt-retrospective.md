# Bug Hunt Rounds 1–3 Retrospective
**Squad:** Fury · Barton · Hill · Romanoff  
**Bugs closed:** 68 total (14 + 28 + 26)  
**Date:** 2026-02-27

## Root Cause Summary

| # | Category | Example Bug | Structural Fix |
|---|----------|-------------|----------------|
| 1 | Dead code | `PerformMinionAttackPhase()` never called | `IDE0051` as WarningsAsError |
| 2 | Missing stat tracking | `RecordRunEnd` on 2/5 death paths | `ExitRun()` single chokepoint |
| 3 | Shared mutable state | `CreateRandomItem()` returning catalog refs | `Item.Clone()` / `record` type |
| 4 | Incomplete feature | Freeze: no apply, no enforce, no-op tick | Enum coverage test |
| 5 | Passive no-ops | WardingRing shows message, applies nothing | Integration test |
| 6 | Data/code mismatch | SetId "ironclad-set" vs "ironclad" | JSON schema + startup validator |
| 7 | Fix regression | Leviathan TurnCount off-by-one | Turn-matrix test table |
| 8 | Partial scope | PassiveEffectProcessor 3/10 slots × 3 | `Enum.GetValues<ArmorSlot>()` |
| 9 | No reset contract | Boss phase flags survive flee | Re-entry test |
| 10 | Wrong API contract | `UseItem()` stacks stats; tests validated it | Fix + test update |
| 11 | Edge case crash | `LootTable.RollDrop()` empty tier pool | Empty-collection test |
| 12 | Property shadowing | `ArchlichSovereign.Phase2Triggered` | `CS0108` as WarningsAsError |
| 13 | Premature validation | Crafting rejects valid crafts at full inv | Order-of-ops test |
| 14 | Missing required param | `HandleDescend` omits `allItems` (floors 2-8 gutted) | Remove optional default |
| 15 | Non-deterministic RNG | `new Random()` in ShieldBash, Mimic | CI grep audit |

## Action Plan

### Immediate (Week 1)
- Add `CS0108 CS0114 CS0169 IDE0051 IDE0052` to `<WarningsAsErrors>` in Dungnz.csproj
- Add RNG audit CI step (grep `new Random()` → build failure)
- Refactor all death paths → `ExitRun(DeathCause)` single chokepoint
- `PassiveEffectProcessor` → `Enum.GetValues<ArmorSlot>()` everywhere
- `DungeonGenerator(List<Item> allItems)` — remove default, compiler-enforced
- `AffixRegistry` exhaustive `throw` on unknown key
- Write 15 gap-filling tests (RunStats, BossVariant, RngDeterminism, Minion, Inventory)

### Week 2
- JSON schema files for `item-stats.json` and `enemy-config.json`
- `ajv validate` CI step
- `ItemConfigValidator.ValidateOrThrow()` + SetId cross-validation in `Program.cs`
- Coverage threshold 62% → 75%
- Full-run integration tests

### Week 3
- `Item` as `record` / add `Clone()`
- Boss turn triggers → `Dictionary<int, BossAction>` data table
- Stryker.NET weekly mutation testing workflow
- CsCheck property-based tests for LootTable, CraftingSystem, DungeonGenerator

### Coverage Targets
- Week 1: 70% | Week 2: 75% | Week 3: 80% | Month 2: 85%
