# Coulson — History Archive (pre-2026-02)

*Archived 2026-03-09. Contains entries older than 3 months.*

---

### 2025-07-24: Combat Item Usage Feature Design

**Requested by:** Anthony  
**Objective:** Add "Use Item" option to combat action menu for consuming potions mid-fight.

**Design Decisions:**
- New `IDisplayService` method: `Item? ShowCombatItemMenuAndSelect(IReadOnlyList<Item> consumables)`
- Combat menu gets 4th option "🧪 Use Item" → "I"; grayed out when no consumables (info line pattern)
- `CombatEngine.HandleItemMenu()` mirrors `HandleAbilityMenu()` pattern
- Reuses `AbilityMenuResult` enum — no new types needed
- `InventoryManager.UseItem` called as-is — no changes required
- Cancel does NOT consume turn (differs from ability cancel per #611)
- `Heal()`/`RestoreMana()` already clamp at max — no overflow handling needed

**Issues Created:**
- #647 — Display: combat item selection menu (assigned Hill)
- #648 — Engine: wire Use Item into CombatEngine (assigned Barton, depends on #647)
- #649 — Tests: combat item usage tests (assigned Romanoff, depends on #647 + #648)

**Decision written to:** `.ai-team/decisions/inbox/coulson-combat-items-design.md`

---

### 2025-07-21: Deep Architecture Audit

**Scope:** Full file-by-file audit of Engine/, Models/, Systems/, Display/, Program.cs
**Trigger:** Anthony requested thorough structural audit beyond prior bug hunt

**Key Findings (19 new issues):**

1. **Boss loot scaling completely broken (P1)** — `HandleLootAndXP` calls `RollDrop` without `isBossRoom` or `dungeonFloor`, so bosses never get guaranteed Legendary drops and floor-scaled Epic chances never fire
2. **Enemy HP can go negative (P1)** — Direct `enemy.HP -= dmg` without clamping inflates DamageDealt stats 
3. **Boss phase abilities skip DamageTaken tracking (P1)** — Reinforcements, TentacleBarrage, TidalSlam all deal damage without incrementing RunStats.DamageTaken
4. **SetBonusManager 2-piece stat bonuses never applied (P1)** — ApplySetBonuses computes totalDef/HP/Mana/Dodge then discards them with `_ = totalDef`
5. **SoulHarvest dual implementation (P1)** — Inline heal in CombatEngine + unused GameEventBus-based SoulHarvestPassive; if bus ever wired, heals double
6. **FinalFloor duplicated 4x across command handlers** — Should be a shared constant
7. **Hazard narration arrays duplicated** — GameLoop and GoCommandHandler have identical static arrays
8. **CombatEngine = 1,709 line god class** — PerformPlayerAttack ~220 lines, PerformEnemyTurn ~460 lines
9. **GameEventBus never wired** — Exists alongside GameEvents; neither fully connected
10. **DescendCommandHandler doesn't pass playerLevel** — Enemies on floor 2+ ignore player's actual level for scaling

**Architecture Patterns Discovered:**
- Two parallel event systems (GameEventBus + GameEvents) coexist without clear ownership
- CommandContext is a 30+ field bag-of-everything that couples all handlers to GameLoop internals
- Boss variant constructors ignore their `stats` parameter, duplicating hardcoded values
- SetBonusManager was designed but never fully wired — stat application is a no-op
- CombatEngine holds the floor via no parameter — floor context is not threaded through for loot

**Key File Paths:**
- `Engine/CombatEngine.cs` — 1,709 lines, combat god class; handles attacks, abilities, boss phases, loot, XP, leveling
- `Engine/Commands/` — Command handler pattern with CommandContext; GoCommandHandler is the main room-transition handler
- `Systems/SetBonusManager.cs` — Manages equipment set bonuses; 2-piece bonuses computed but discarded
- `Systems/SoulHarvestPassive.cs` — Dead code (GameEventBus never instantiated in Program.cs)
- `Engine/StubCombatEngine.cs` — Dead code from early development
- `Models/LootTable.cs` — Static tier pools, RollDrop with isBossRoom/dungeonFloor params that callers don't use

**Artifacts:**
- `.ai-team/decisions/inbox/coulson-deep-dive-audit.md` — Full 19-finding audit report with file/line references

---

### 2025-07-21: Terminal.Gui Migration Architecture Design

**Scope:** Complete architectural design and issue decomposition for migrating Dungnz display layer from Spectre.Console to Terminal.Gui v2.

**Key Architectural Decisions:**

1. **Dual-Thread Model (AD-1):** Terminal.Gui event loop on main thread, game logic (GameLoop, CombatEngine, all command handlers) on background thread. Display methods marshal to UI thread via `Application.Invoke()`. Input-coupled methods block game thread via `TaskCompletionSource<T>` until TUI dialog returns. This approach requires ZERO changes to existing game logic — GameLoop.RunLoop(), CombatEngine.RunCombat(), and all 20+ command handlers remain unchanged.

2. **Feature Flag (AD-2):** `--tui` CLI argument selects Terminal.Gui; default remains Spectre.Console. Rollback = delete `Display/Tui/` + revert 2 files.

3. **Additive Only (AD-3):** All Terminal.Gui code lives in `Display/Tui/` as new files. IDisplayService, SpectreDisplayService, IInputReader, GameLoop, CombatEngine are NOT modified.

4. **Split-Screen Layout (AD-5):** Five panels — Map (top-left), Stats+Equipment (top-right), Content (middle), Message Log (lower), Command Input (bottom). Percentage-based positioning for terminal resize support.

5. **Input-Coupled Method Strategy (AD-6):** All 19+ input-coupled methods use `TuiMenuDialog<T>` modal dialogs. Game thread creates `TaskCompletionSource<T>`, posts dialog to UI thread, blocks until user selects. Consistent pattern across all selection methods.

**Key File Paths (new):**
- `Display/Tui/TuiLayout.cs` — Main split-screen window with 5 panels
- `Display/Tui/TerminalGuiDisplayService.cs` — IDisplayService implementation for Terminal.Gui
- `Display/Tui/TerminalGuiInputReader.cs` — IInputReader using BlockingCollection for thread bridging
- `Display/Tui/TuiMenuDialog.cs` — Reusable modal dialog for all input-coupled methods
- `Display/Tui/Panels/MapPanel.cs` — ASCII dungeon map rendering
- `Display/Tui/Panels/StatsPanel.cs` — Live player stats + equipment
- `Display/Tui/Panels/ContentPanel.cs` — Main narrative/display area
- `Display/Tui/Panels/MessageLogPanel.cs` — Scrollable message history

**Key File Paths (existing, analyzed):**
- `Display/IDisplayService.cs` — 413 lines, 85+ methods, 19+ input-coupled (marked with remarks)
- `Display/SpectreDisplayService.cs` — 69KB, ~1500 lines, full Spectre.Console implementation
- `Engine/GameLoop.cs` — Sync `while(true)` loop, reads command via `_input.ReadLine()`, dispatches to command handlers
- `Engine/CombatEngine.cs` — 1709-line blocking combat engine, uses IDisplayService + IInputReader
- `Engine/StartupOrchestrator.cs` — Pre-game menu flow, uses input-coupled IDisplayService methods
- `Engine/IntroSequence.cs` — Gathers name/class/difficulty via input-coupled methods
- `Engine/IInputReader.cs` — `ReadLine()`, `ReadKey()`, `IsInteractive` — ConsoleInputReader wraps Console

**Patterns Established:**
- Thread bridging pattern: `BlockingCollection<T>` for game-thread ↔ UI-thread communication
- Input-coupled method pattern: `TaskCompletionSource<T>` + `Application.Invoke()` + modal dialog
- UI marshaling pattern: all Terminal.Gui writes via `Application.Invoke()` from game thread
- Rollback pattern: feature flag + additive-only code in isolated directory

**Issue Decomposition (13 issues):**
- Epic: #1015
- Phase 1 Foundation: #1016 (Fitz), #1017 (Hill), #1018 (Hill), #1019 (Hill), #1020 (Hill), #1021 (Hill)
- Phase 2 Panels: #1022 (Barton), #1023 (Barton), #1024 (Hill), #1025 (Barton)
- Phase 3 Integration: #1026 (Hill), #1027 (Romanoff), #1028 (Fitz)

**Work Distribution:**
- Hill: 7 issues (layout, thread bridge, display service, content panel, integration)
- Barton: 3 issues (map panel, stats panel, message log panel)
- Romanoff: 1 issue (integration testing)
- Fitz: 2 issues (project setup, documentation)

**Artifacts:**
- `.ai-team/decisions/inbox/coulson-terminal-gui-architecture.md` — Full architecture document with 6 architectural decisions, threading model, layout diagrams, rollback strategy
- GitHub issues #1015–#1028 — Complete issue set with descriptions, acceptance criteria, dependencies, and assignees
