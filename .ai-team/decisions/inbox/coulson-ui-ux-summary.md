# UI/UX Improvement Plan — Executive Summary

**Date:** 2026-02-20  
**Lead:** Coulson  
**Status:** Ready for team review and Boss approval

---

## The Problem

TextGame currently displays everything in **plain white text**. While the game uses emoji and Unicode box-drawing for visual distinction, there's no color system, no real-time status tracking, and limited visual feedback for player actions.

---

## The Solution

Implement a comprehensive UI/UX enhancement through **3 phases** of improvements:

### Phase 1: Foundation (5-7 hours)
- ANSI color system core
- DisplayService color methods
- Core stat colorization (HP, Mana, Gold, XP, Attack, Defense)

### Phase 2: Enhancement (6-8 hours)
- Combat visual hierarchy (colored damage/healing/crits)
- Enhanced combat HUD with active effects
- Equipment comparison display
- Inventory weight tracking
- Status effect summary panel

### Phase 3: Polish (4-5 hours)
- Achievement progress tracking
- Enhanced room descriptions
- Ability cooldown visual indicators
- Combat turn log improvements

**Total Estimate:** 15-20 hours

---

## Key Improvements At-a-Glance

| Feature | Before | After |
|---------|--------|-------|
| **HP Display** | `45/60` (white) | `45/60` (green/yellow/red based on %) |
| **Combat Status** | `[You: 45/60 HP] vs [Goblin: 12/30 HP]` | `[You: 45/60 HP │ 15/30 MP │ P(2) R(3)] vs [Goblin: 12/30 HP │ W(2)]` |
| **Damage Messages** | `You strike Goblin for 15 damage!` | `You strike Goblin for **15** damage!` (red highlight) |
| **Equipment** | `You equipped Iron Sword.` | Shows before/after stats with colored deltas |
| **Inventory** | Lists items only | Shows slots, weight, value with threshold colors |
| **Abilities** | Lists all abilities | Colors ready abilities green, cooling abilities gray |
| **Achievements** | Shows unlocked only | Shows progress toward locked achievements |

---

## Color Palette

| Element | Color | Purpose |
|---------|-------|---------|
| HP | Red (threshold-based) | Health status at-a-glance |
| Mana | Blue | Mana/resource tracking |
| Gold | Yellow | Currency |
| XP | Green | Experience gains |
| Attack | Bright Red | Offensive stats |
| Defense | Cyan | Defensive stats |
| Success | Green | Confirmations, healing |
| Errors | Red | Warnings, failures |
| Cooldowns | Gray | Disabled abilities |

---

## Architecture Impact

✅ **No breaking changes** — All improvements via DisplayService extensions  
✅ **Clean separation** — Game logic untouched; only display layer modified  
✅ **Test-friendly** — TestDisplayService strips ANSI codes automatically  
✅ **Accessible** — Color enhances existing emoji/labels, never replaces  

---

## Team Workload

- **Hill:** 8-10 hours (Color system, core colorization, inventory/equipment display)
- **Barton:** 7-9 hours (Combat hierarchy, HUD, status effects, ability visuals)
- **Romanoff:** 3-4 hours (Test infrastructure updates, verification)
- **Coulson:** 2-3 hours (Design review, code review, approval gates)

**Total:** 20-26 hours

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| ANSI support variance | Auto-detect + graceful fallback |
| Test breakage | Strip ANSI codes in TestDisplayService |
| Color readability | High-contrast palette, tested on multiple terminals |

---

## Success Metrics

- [ ] All 267 tests pass (zero regressions)
- [ ] Visual clarity: HP state, effects, cooldowns instantly recognizable
- [ ] Information density: All actionable info visible without scrolling
- [ ] Accessibility: Color-blind players retain full experience via emoji/labels

---

## Next Steps

1. **Team Design Review** — Present full plan to Hill, Barton, Romanoff
2. **Boss Approval** — Confirm scope and priorities
3. **Phase 1 Kickoff** — Hill implements ColorCodes utility + DisplayService extensions
4. **Parallel Phase 2 Work** — Hill (inventory/equipment), Barton (combat/status)
5. **Phase 3 Polish** — Both engineers tackle remaining enhancements
6. **Final Review** — Coulson validates architecture decisions before merge

---

**Full Plan:** `.ai-team/decisions/inbox/coulson-ui-ux-architecture.md` (20KB)
