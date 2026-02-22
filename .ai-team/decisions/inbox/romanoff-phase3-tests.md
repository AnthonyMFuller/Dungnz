# Decision: Phase 3 Loot UX — Test Coverage Scope

**Author:** Romanoff (Tester)  
**Date:** 2026-02-20  
**Status:** Informational — documents test coverage for Phase 3 looting UX polish

---

## Context

Hill implemented Phase 3 looting UX in `ConsoleDisplayService` and `IDisplayService` (grouping, elite loot, weight warning, "vs equipped" indicator). Romanoff was tasked with writing proactive tests for these features.

**File written:** `Dungnz.Tests/Phase3LootPolishTests.cs`  
**Tests:** 17 unit tests, all passing against current production code.

---

## Coverage Scope

### 3.1 — Consumable Grouping (`ShowInventory`)
| Test | What it verifies |
|---|---|
| Three identical potions → `×3` | Grouping produces correct multiplier badge |
| Different-named items stay separate | Grouping is name-only; no cross-type merging |
| Single item → no `×` badge | Multiplier only shown for count > 1 |
| Empty inventory → no `×` artifacts | Edge case: empty state produces clean output |

### 3.2 — Elite Loot Callout (`ShowLootDrop`)
| Test | What it verifies |
|---|---|
| `isElite: true` → "ELITE LOOT DROP" | Elite flag surfaces distinct header |
| `isElite: false` → "LOOT DROP" not "ELITE" | Normal drops never contaminated with "ELITE" |
| Uncommon item → "[Uncommon]" badge | Tier badge rendered for Tier 2 |
| Rare item → "[Rare]" badge | Tier badge rendered for Tier 3 |
| Common item → "[Common]" badge | All tiers explicitly labeled |

### 3.3 — Weight Warning (`ShowItemPickup`)
| Test | What it verifies |
|---|---|
| 85% weight → ⚠ + "nearly full" | Warning fires above threshold |
| 79% weight → no ⚠ | No false positives below threshold |
| Exactly 80% weight → no ⚠ | Boundary is strict `>` (not `>=`); 80% is safe |
| 82% weight → ⚠ | Just-over-boundary confirms threshold fires correctly |

### 3.4 — New Best Indicator (`ShowLootDrop`)
| Test | What it verifies |
|---|---|
| Attack +5 drop, +2 equipped → "+3 vs equipped" | Positive delta shows improvement and magnitude |
| Attack +5 drop, +5 equipped → no "vs equipped" | No improvement → no indicator |
| Attack +3 drop, +5 equipped → no "vs equipped" | Downgrade → no indicator |
| No weapon equipped → no "vs equipped" | Null guard: nothing to compare against |

---

## Key Design Observations Documented in Tests

1. **Exact 80% boundary is exclusive** — `ShowItemPickup` uses `weightCurrent > weightMax * 0.8` (strict greater-than). Exactly 80% does NOT trigger the warning. Tests document both sides of this boundary. If the threshold is ever changed to inclusive (`>=`), the boundary test must be updated.

2. **"vs equipped" only triggers for positive delta, weapon-type items, with a weapon equipped** — Non-weapon items (armor, accessories) do not trigger the comparison. Neither do zero-delta or negative-delta cases.

3. **Grouping is name-based only** — Items with the same `Name` but different stats would be grouped under the current implementation. This is acceptable for Phase 3 scope but is a potential edge case if stats ever diverge for same-name items (crafted upgrades, etc.).

---

## Pre-existing Build Issue Fixed

`TierDisplayTests.cs` line 390 had a `CS1744` error (FluentAssertions `ContainAny` named-arg conflict). Fixed by removing the redundant `because:` argument. This was blocking the test project from compiling and was within Romanoff's test-infrastructure ownership.

---

## Verdict

Phase 3 test coverage is complete. All 17 tests pass. Hill's implementation is confirmed correct against all specified Phase 3 behaviors.
