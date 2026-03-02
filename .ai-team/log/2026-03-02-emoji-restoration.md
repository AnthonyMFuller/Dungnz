# Session: Emoji Restoration (2026-03-02)

**Requested by:** Anthony (Boss)

## Context

Anthony requested restoring original visual emojis, overturning the decision from PR #830 to use narrow symbols for alignment.

## Work Completed

**Hill restored all wide EAW=W emojis:**  
💍🪖🥋🧤👖👟🧥⭐✨🏃🧪

**Chest/Armor slot:** Replaced 🛡 (EAW=N, caused misalignment) with 🦺 (EAW=W, visually consistent)

**Weapon and Off-Hand:** Anthony requested both also use EAW=W emojis
- Hill changed: ⚔→🔪 (Weapon), ⛨→🔰 (Off-Hand)

**NarrowEmoji set simplified:**  
Removed ⛨, now only `{"⚔","⚗","☠","★","↩","•"}` used in combat menu Attack label

## Outcome

**PR #833 merged to master (decf902)**  
**Issue #832 closed**

All wide emoji slots now visually consistent with EAW=W (2 terminal columns), while preserving the critical EL() helper fix for narrow symbols.
