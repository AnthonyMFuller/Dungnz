### 2026-03-11: Spectre markup bracket sweep complete
**By:** Barton
**What:** All unescaped [WORD] patterns in Display/, Engine/, Systems/ swept and fixed
**Why:** Retro P0 — [CHARGED] crash class permanently closed

## Sweep Results

### Grep patterns executed
```bash
grep -rn '\[[A-Z_][A-Z_]*\]' Dungnz.Display/ --include="*.cs"
grep -rn '\[[A-Z_][A-Z_]*\]' Dungnz.Engine/ --include="*.cs"
grep -rn '\[[A-Z_][A-Z_]*\]' Dungnz.Systems/ --include="*.cs"
grep -rn '".*\[.*{' Dungnz.Display/ --include="*.cs"
```

### Findings

**Display/ — ALL CLEAN (no fixes needed)**
- `SpectreLayoutDisplayService.cs` L449, L507: `[[CHARGED]]` — already escaped with double brackets ✅
- `SpectreDisplayService.cs` L609-651: all map legend entries (`[[B]]`, `[[E]]`, etc.) — already escaped ✅
- `DisplayService.cs` L1089: `[EQUIPMENT]` — Console.WriteLine, not Spectre markup ✅
- `DisplayService.cs` L1544: `[SHIELD ACTIVE]` — Console.WriteLine path, not Spectre ✅
- `SpectreDisplayService.cs` L1169: `[SHIELD ACTIVE]` — wrapped in `Markup.Escape(ctx.ToString())` ✅

**Engine/ — ALL CLEAN (no fixes needed)**
- `CombatEngine.cs` L434, L448: `[A]ttack`, `[B]ability`, `[F]lee` — via `ShowMessage`/`ShowError` which call `Markup.Escape` ✅
- `CombatEngine.cs` L464, L469: `[SHIELD ACTIVE]`, `[DIVINE SHIELD: NT]` — via `ShowMessage` + `Markup.Escape` ✅
- `CombatEngine.cs` L954: `[Battle Hardened]` — via `ShowCombatMessage` + `ConvertAnsiInlineToSpectre` (always escapes plain text) ✅
- `AttackResolver.cs` L133, L158, L199, etc.: `[Focus]`, `[Fury]`, `[Shadowstep]` etc. — via `ShowColoredCombatMessage` + `Markup.Escape` ✅

**Systems/ — ALL CLEAN (no fixes needed)**
- `StatusEffectManager.cs` L59: `[Cursed]` — via `ShowCombatMessage` + `ConvertAnsiInlineToSpectre` ✅
- `AbilityManager.cs` L852: `[Arcane Charge]` — via `ShowColoredCombatMessage` + `Markup.Escape` ✅

### Why no code changes were required

All display methods that accept game-state strings (`ShowMessage`, `ShowError`, `ShowCombatMessage`, `ShowColoredCombatMessage`) were previously hardened to call `Markup.Escape` or `StripAnsiCodes` + `Markup.Escape` before rendering. The `ConvertAnsiInlineToSpectre` converter also escapes all plain-text segments.

All places in Display/ that build Spectre markup directly from game state (status effects, names, items) consistently use `Markup.Escape(x.ToString())`. All map symbols use `[[X]]` double-bracket escaping.

### Key architectural protection

The crash-proof design is:
1. `ShowMessage` / `ShowError` → `Markup.Escape(StripAnsiCodes(message))`
2. `ShowCombatMessage` → `ConvertAnsiInlineToSpectre` → always escapes plain text segments
3. `ShowColoredCombatMessage` → `Markup.Escape(StripAnsiCodes(message))`
4. Direct markup builders → `Markup.Escape(dynamicValue)` for every game-state string
5. Map symbols → `[[X]]` double-bracket notation
