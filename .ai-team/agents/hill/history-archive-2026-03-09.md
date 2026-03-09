# Hill — History Archive (pre-2026-02)

*Archived 2026-03-09. Contains entries older than 3 months.*

---

### 2025 — Emoji Label Audit (#820, #821, #822)

**Issues Closed:** #820, #821, #822
**File Modified:** `Display/SpectreDisplayService.cs`

## Learnings

**What was found:**
- Line 233: `table.AddRow("⚡ Combo", ...)` — ⚡ is in `NarrowEmoji` but was using raw string (1 space instead of 2)
- Line 766: `table.AddRow("⭐ Level", ...)` — ⭐ is NOT in `NarrowEmoji` (wide emoji, 1 space is correct), but was using raw string instead of `EL()`
- All other emoji+text labels in table rows and menus were already using `EL()` (equipment slots, combat actions)

**What was fixed:**
- Line 233: Updated to `EL("⚡", "Combo")` — now correctly gets 2 spaces (narrow emoji)
- Line 766: Updated to `EL("⭐", "Level")` — gets 1 space (wide emoji, correct behavior)

**Key decision:** ⭐ (U+2B50) is a wide emoji and was NOT added to `NarrowEmoji`. EL() gives it 1 space, which is correct for terminal rendering.

**Build:** `dotnet build` passes with 0 errors (3 pre-existing XML doc warnings, unrelated).
