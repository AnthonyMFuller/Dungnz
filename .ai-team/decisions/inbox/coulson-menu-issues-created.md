### 2026-03-06: Menu bug GitHub issues created
**By:** Coulson
**What:** Created 6 GitHub issues for menu/UI bugs found in independent audit
**Why:** Boss requested deep menu bug hunt with issue creation before fixes

## Audit Methodology

Conducted independent code review of:
- All files in `Display/Spectre/`
- All command handlers in `Engine/Commands/`
- `Display/IDisplayService.cs` interface
- Special room handlers in `Engine/GameLoop.cs`

**Focus areas:**
1. Handlers showing menus but not calling ShowRoom() on exit paths
2. ContentPanelMenu<T> / ContentPanelMenuNullable<T> return value handling
3. Commands leaving input panel in broken/frozen state
4. Multi-step menu flows with unclean cancel paths
5. Edge cases: empty inventory, 0 gold, empty shop stock

## Issues Created

| Issue # | Severity | Title | Location |
|---------|----------|-------|----------|
| #1162 | Critical | ShopCommandHandler missing ShowRoom() on empty stock error path | Engine/Commands/ShopCommandHandler.cs:47 |
| #1163 | High | CraftCommandHandler missing ShowRoom() on cancel | Engine/Commands/CraftCommandHandler.cs:22 |
| #1164 | Critical | HandleShrine missing ShowRoom() on all exit paths | Engine/GameLoop.cs:363-443 |
| #1165 | Critical | HandleForgottenShrine missing ShowRoom() on all exit paths | Engine/GameLoop.cs:445-471 |
| #1166 | Critical | HandleContestedArmory missing ShowRoom() on all exit paths | Engine/GameLoop.cs:506-568 |
| #1167 | Critical | HandleTrapRoom missing ShowRoom() on all exit paths | Engine/GameLoop.cs:570-680 |

## Pattern Identified: Systematic ShowRoom() Omission

**Root cause:** Special room handlers in GameLoop.cs (shrines, armories, trap rooms) were added after the command handler pattern was established. They show menus via IDisplayService but don't follow the command handler convention of calling ShowRoom() before returning to the game loop.

**Impact:** 5 out of 6 Critical bugs are in GameLoop.cs special room handlers. These rooms are high-value gameplay moments (shrines, armories, traps) where players make important decisions, but the UX is broken — content panel freezes after every interaction.

**Command handlers affected:**
- ShopCommandHandler: 1 missing path (empty stock edge case)
- CraftCommandHandler: 1 missing path (cancel)

**GameLoop special handlers affected (100% broken):**
- HandleShrine: 0 ShowRoom() calls on 9 exit paths
- HandleForgottenShrine: 0 ShowRoom() calls on 4 exit paths  
- HandleContestedArmory: 0 ShowRoom() calls on 6 exit paths
- HandleTrapRoom: 0 ShowRoom() calls on ~12 exit paths (3 variants × 4 paths each)

## Architectural Recommendation

**Problem:** No compile-time enforcement that menu-showing code restores display state.

**Proposed solution (for post-fix architectural work):**
1. Extract a `IMenuHandler` interface with `BeforeMenu()` / `AfterMenu()` lifecycle hooks
2. Make `AfterMenu()` automatically call ShowRoom() unless explicitly suppressed
3. Or: Make all `*AndSelect` methods accept a restoration callback

This is a "design smell" — the same bug repeated 31 times (31 exit paths missing ShowRoom) indicates a missing architectural guard-rail.

## Romanoff's Findings

Checked for `.ai-team/decisions/inbox/romanoff-menu-bug-audit.md` — **file does not exist yet**. Proceeding with just my own findings as instructed.

## Related Work

Previous merchant menu bug triage (coulson-merchant-menu-bugs.md) identified 4 bugs:
- #1157: SellCommandHandler doesn't call ShowRoom() after sale (fixed)
- #1158: SellCommandHandler should loop for multiple sales (enhancement)
- #1156: ShopCommandHandler doesn't restore on Leave (fixed)
- #1159: ContentPanelMenu Escape returns wrong value (fixed)

**Status of previous issues:** Appear to have been resolved. Today's audit found NEW issues in previously unaudited code (special room handlers).

## Priority Recommendation

**Critical issues (fix immediately):**
- #1164: HandleShrine (most common special room type)
- #1165: HandleForgottenShrine  
- #1166: HandleContestedArmory
- #1167: HandleTrapRoom

**High priority:**
- #1163: CraftCommandHandler cancel

**Lower priority:**
- #1162: ShopCommandHandler empty stock (edge case, happens after buying entire stock)

**Estimated fix time:** 2-3 hours (mechanical fixes, all follow same pattern)
**Recommended owner:** Hill or Barton (both familiar with GameLoop.cs)
