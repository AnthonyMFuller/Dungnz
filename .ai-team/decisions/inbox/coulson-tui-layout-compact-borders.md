### 2026-03-06: TUI layout compactness and border alignment fixes

**By:** Coulson

**What:** Created two GitHub issues (#1091, #1092) documenting TUI display bugs:
1. Border alignment issues caused by emoji width miscalculation in panel headers
2. Excessive vertical space usage due to Content panel taking 50% of screen height

**Why:** Anthony reported visual quality issues with the Spectre.Console TUI that impact player experience. The border misalignment creates a "janky" appearance, and the oversized Content panel wastes screen real estate. Both issues are architectural — they stem from design choices in `SpectreLayout.cs` and `SpectreLayoutDisplayService.cs` that didn't account for emoji rendering behavior or typical content density.

**Root causes identified:**
- **Border alignment**: Emojis (🗺, ⚔) are 1 character in markup but render as 2-cell width in terminals. Spectre.Console's header centering calculation doesn't account for this, causing 1-2 character misalignment.
- **Vertical space**: Hard-coded ratio values give Content panel 50% (ratio 5/10) of screen height. At 50 rows, this allocates 25 rows to scrolling narrative text while Map/Stats get only 15 and Log/Input get 10. The Content density doesn't justify half the screen.

**Recommended fixes:**
- **Issue #1091**: Remove emojis from headers (`Floor N`, `Player Stats`) or add compensating spaces. Emoji removal preferred for cross-terminal compatibility.
- **Issue #1092**: Reduce Content ratio from 5 to 4 (50% → 40%) and increase BottomRow ratio from 2 to 3 (20% → 30%). This gives more visible message log history without cramping the map.

**Assignment:** Issues created with `bug` label, no assignee. Hill or Barton can pick up as quick wins between larger features.
