# Session: Message Log Cap + Command Input MinimumSize

**Requested by:** Anthony (Boss)  
**Agent:** Hill  
**Date:** 2026-03-06

---

## Summary

Hill fixed two critical UI bugs in a single session:

1. **Message Log Display Cap** — Messages now limited to 12 most recent (MaxDisplayedLog=12 constant). Buffer remains at 50 for history retention.
2. **Command Panel Collapse Prevention** — Input layout (Command panel) now has MinimumSize(4) to prevent collapse on very small terminals.

---

## GitHub Issues

- **#1143**: Message Log display improvements → closed
- **#1144**: Command Input panel minimum size → closed

Both created and closed in same session.

---

## Commit Reference

**Commit:** fc26b10  
**Branch:** master  
**Type:** Bug fix

---

## Impact

- **Terminal UX:** Improved readability on small screens
- **Buffer efficiency:** Large message histories preserved without overwhelming display
- **Robustness:** Prevents layout collapse in edge cases
