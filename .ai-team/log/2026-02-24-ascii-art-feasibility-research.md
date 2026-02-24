# Session: ASCII Art Enemy Encounter — Feasibility Research

**Date:** 2026-02-24  
**Requested by:** Anthony  
**Status:** Research Complete

---

## Summary

Anthony requested the team research the feasibility of adding ASCII art for enemies when encountered during combat. Three specialists (Coulson, Hill, Barton) were spawned in parallel to investigate from their respective angles.

**Verdict:** Unanimously recommended to proceed.

---

## Team Findings

### Coulson (Architecture Lead)

ASCII art for enemy encounters is **architecturally feasible and low-risk**. The display layer is already well-abstracted and supports multi-line output blocks (title screen, class cards, equipment cards, loot drops all demonstrate this pattern). 

**Key Details:**
- **Natural insertion point:** `ShowCombatStart(Enemy enemy)` method, which receives the enemy object and already displays complex multi-line structures
- **Art dimensions:** 30–42 chars wide, 5–10 lines tall (fits the 80-char terminal baseline with safe margins)
- **Phase 1 effort:** ~2 work items, ~6–8 hours of implementation + testing
- **Risks:** Terminal width compatibility, ANSI color portability, test fragility — all manageable with straightforward mitigations
- **Recommendation:** Phase 1 should focus on simple, hardcoded portrait templates per enemy type (zero file I/O, fast iteration)

**Phase 2 possibilities:** Color theming per enemy type, elite/boss variants, animated frames.

### Hill (C# Display Layer Expert)

Display layer is **fully additive-ready** for ASCII art integration. No changes to `IDisplayService` interface are required.

**Key Details:**
- **New method fit:** `ShowEnemyArt(string[] artLines)` integrates naturally alongside existing display methods
- **Box width compatibility:** Current card widths are 36–44 chars; ASCII art at 30–42 chars fits seamlessly
- **ANSI color integration:** Existing `ColorCodes` utility + `PadRightVisible` helper make color + alignment straightforward
- **Testing infrastructure:** `FakeDisplayService` stub for the new method is ~4–5 lines

### Barton (Systems/Data Expert)

Enemy data is **well-positioned for ASCII art addition** via data-driven approach.

**Key Details:**
- **Inventory:** 18 regular enemy types + 5 bosses defined in `enemy-stats.json`
- **Pattern match:** Add `AsciiArt: [...]` field directly to enemy JSON entries (follows existing convention for `BossNarration` system)
- **Encounter insertion point:** `CombatEngine.RunCombat()`, after `ShowCombatStart()`
- **Content scope:** ~70–100 total lines of ASCII art needed across all 23 enemy types
- **No conflicts:** Existing boss `BossNarration` system and encounter logic are unaffected

---

## Team Recommendation

**Proceed with Phase 1 implementation.** ASCII art is a low-risk, high-flavor addition that leverages existing infrastructure and creates an immediate visual improvement to combat encounters.

**Next steps (when Anthony decides to proceed):**
1. Art design: Sketch 1–2 enemy portraits as PoC
2. Integration: Implement art rendering in `ShowCombatStart`
3. Testing: Add non-snapshot tests for output validation
4. Iterate: Gather feedback from playtesting
