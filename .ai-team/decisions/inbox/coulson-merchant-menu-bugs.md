### 2025-04-14: Merchant menu bug triage
**By:** Coulson
**What:** Identified 4 bugs in merchant sell/shop flow. Issues created (#1157, #1158, #1156, #1159).
**Why:** User reported sell confirm menu persisting after sale. Root cause analysis revealed missing `ShowRoom()` calls on exit paths and `ContentPanelMenu` Escape returning wrong value. These bugs break the merchant interaction UX and create accidental action confirmations.

## Bugs Identified

| Issue | Priority | Title | Root Cause | Fix |
|-------|----------|-------|-----------|-----|
| #1157 | Critical | Sell confirm menu persists after successful sell | `SellCommandHandler` doesn't call `ShowRoom()` after sale | Call `context.Display.ShowRoom()` on success path |
| #1158 | Enhancement | SellCommandHandler should allow selling multiple items | Handler returns after first sale, forces replay of `sell` command | Wrap flow in `while(true)` loop like `ShopCommandHandler` |
| #1156 | High | ShopCommandHandler doesn't restore room view on Leave | No `ShowRoom()` call on Leave path | Call `context.Display.ShowRoom()` before return on Leave |
| #1159 | High | ContentPanelMenu Escape returns selected instead of cancel | Escape/Q returns `items[selected]` not the cancel sentinel | Change to return `items[items.Count - 1]` (last item = cancel) |

## Context
This triage is part of the ongoing merchant interaction improvements. These bugs represent systematic issues:
1. **Pattern failure**: Command handlers must call `ShowRoom()` on exit to restore content panel state
2. **UX convention**: Menu items last element is always the cancel option; Escape should return it
3. **Loop pattern**: Interactive sell/shop flows need loops to allow multiple transactions

## Files Affected
- `Engine/Commands/SellCommandHandler.cs`
- `Engine/Commands/ShopCommandHandler.cs`
- `Display/Spectre/SpectreLayoutDisplayService.Input.cs`

## Next Steps
- Hill or Barton: Pick up issues #1157, #1156, #1159 (bugs)
- Hill or Barton: Pick up issue #1158 (enhancement)
- Ensure all fix commits reference the issue number
