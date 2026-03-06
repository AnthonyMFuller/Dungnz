### 2026-03-05: Deep UI Bug Hunt — Menu/Input State Issues

**By:** Coulson  
**Trigger:** Boss reported player unable to cancel inventory menu and command input frozen afterward  
**Scope:** Full audit of `SpectreLayoutDisplayService.Input.cs`, `SpectreLayoutDisplayService.cs`, `SpectreLayout.cs`, `GameLoop.cs`, and all menu-related command handlers

---

#### Issues Created

| # | Priority | Title | Assignee |
|---|----------|-------|----------|
| #1129 | **P0** | ReadCommandInput null return falls through to Console.ReadLine, corrupting Live display | Hill |
| #1130 | **P1** | No Escape key handling in ContentPanelMenu — menus cannot be cancelled with Escape | Hill |
| #1131 | **P1** | Content panel not restored after menu cancel — stale menu persists (6 handlers) | Barton |
| #1132 | **P1** | Empty inventory command gives zero feedback — silent no-op | Barton |
| #1133 | **P2** | PauseAndRun uses fragile Thread.Sleep(100) instead of synchronization handshake | Hill |
| #1134 | **P2** | PauseAndRun + AnsiConsole.Prompt can deadlock when Live holds exclusivity lock | Hill |
| #1135 | **P2** | ContentPanelMenu returns first item (not cancel) when ReadKey returns null | Hill |
| #1136 | **P2** | EquipmentManager.HandleEquip cancel path doesn't set TurnConsumed = false | Barton |
| #1137 | **P2** | Shop while(true) loop traps player if merchant stock is empty | Barton |
| #1138 | **P2** | ForgottenShrine menu labels don't match handler logic | Hill |
| #1139 | **P2** | ContestedArmory menu labels don't match handler logic | Hill |
| #1140 | **P3** | Duplicate TierColor/InputTierColor and PrimaryStatLabel/InputPrimaryStatLabel helpers | Hill |

#### Root Cause Analysis

The reported bug ("can't type after canceling inventory") is a **compound failure** of three interacting bugs:

1. **#1130 (Escape key):** User presses Escape expecting to cancel — nothing happens. User perceives the menu as "stuck."
2. **#1131 (Content panel):** Even after canceling via "← Cancel", the Content panel still shows the stale menu. Looks broken.
3. **#1129 (Console.ReadLine fallback):** If the user then presses Enter with empty input, ReadCommandInput returns null and falls through to `Console.ReadLine()`, which corrupts the Live terminal display. After this, the Command panel appears permanently unresponsive.

**#1129 is the critical fix.** It directly causes the "can't type" symptom. #1130 and #1131 cause the user confusion that leads them into the #1129 trap.

#### Recommended Fix Order

1. **#1129** — Immediate: prevent Console.ReadLine fallback (one-line fix in GameLoop.cs)
2. **#1130** — Same sprint: add Escape key handling to all three menu loops
3. **#1131** — Same sprint: add ShowRoom on cancel paths in 6 command handlers
4. **#1132** — Same sprint: show inventory display for empty inventory
5. Remaining P2/P3 issues in next sprint
