# Session: Merchant Menu Bug Fixes - 2026-03-06

**Requested by:** Boss (Copilot)

## Team Actions

### Coulson — Code Audit & Issue Creation
- Audited merchant menu code in `Engine/Commands/` and `Display/Spectre/`
- Identified systematic patterns in sell/shop flow bugs
- Created GitHub issues #1156–#1159 documenting root causes and fixes
- Decision file: `.ai-team/decisions/inbox/coulson-merchant-menu-bugs.md`

### Romanoff — Test Development & Mocking Enhancement
- Wrote 8 tests for merchant menu scenarios
- Enhanced `FakeDisplayService` with queue-based mocking infrastructure
- Added `ShowRoomCallCount` property to verify room state restoration
- Tests validate fix verification strategy for all 4 bugs

### Hill — Bug Fixes
- Fixed bug #1157: `SellCommandHandler` now calls `ShowRoom()` after sale
- Fixed bug #1156: `ShopCommandHandler` calls `ShowRoom()` on Leave path
- Fixed bug #1159: `ContentPanelMenu.Input.cs` Escape returns cancel sentinel (last item)
- Fixed bug #1158: `SellCommandHandler` wrapped in loop for multiple item sales
- All tests passing: 1695 tests
- PR #1160 merged with all fixes

## Outcomes

✅ All 4 merchant menu bugs fixed  
✅ All 4 GitHub issues (#1156–#1159) closed  
✅ 1695 tests passing  
✅ PR #1160 merged  

## Technical Notes

**Bug patterns addressed:**
- Command handlers must call `ShowRoom()` on all exit paths to restore content panel
- Menu cancel option must be last item; Escape returns `items[items.Count - 1]`
- Interactive sell/shop flows need loops to support multiple transactions

**Files modified:**
- `Engine/Commands/SellCommandHandler.cs`
- `Engine/Commands/ShopCommandHandler.cs`
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs`

---

*Session logged by Scribe on 2026-03-06*
