### 2026-03-01: All icons/emoji must belong to the same character set
**By:** Anthony (via Copilot)
**What:** All emoji and icon characters used throughout the game (equipment slots, combat menus, stats display, etc.) must be drawn from a single, consistent Unicode character set. Mixing wide emoji (U+1F000+ range) with narrow text symbols (U+2500-U+26FF range) is explicitly prohibited.
**Why:** Mixed character sets cause terminal column width discrepancies between Spectre.Console's cell measurement and actual terminal rendering, producing persistent border and text alignment bugs that are difficult to fix case-by-case.
