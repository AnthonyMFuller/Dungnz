### 2025: Floor ascension feature issues created
**By:** Coulson  
**What:** Created 7 GitHub issues for floor ascension feature  
**Why:** Boss requested ability to ascend floors; feature is feasible and fully designed

## Issues Created

- #1148 — feat: add IsEntrance property to Room model and tag start room in DungeonGenerator
- #1149 — feat: add FloorHistory to CommandContext for multi-floor state tracking
- #1154 — feat: implement AscendCommandHandler
- #1151 — feat: persist floor history across save/load
- #1152 — feat: add entrance marker [^] to minimap for ascendable start rooms
- #1150 — feat: add ascension narration and update help text
- #1153 — test: add tests for floor ascension feature

## Implementation Sequence

Recommend implementing in this order (dependencies):
1. #1148 (IsEntrance model property) — foundational
2. #1149 (FloorHistory tracking) — extends CommandContext
3. #1154 (AscendCommandHandler) — uses both above
4. #1151 (Save/load persistence) — depends on FloorHistory structure
5. #1152 (Minimap display) — polish
6. #1150 (Narration & help) — documentation
7. #1153 (Tests) — validation, runs parallel with implementation

## Technical Notes

- **Design decision:** Store exit room objects in FloorHistory (not room IDs) to maintain room state across floors
- **Save/load migration:** Bump SaveData from v1 to v2; implement MigrateV1ToV2 for backward compatibility
- **Display changes:** Both Spectre and non-Spectre display services need entrance marker support
- **Handler parity:** AscendCommandHandler mirrors DescendCommandHandler structure for consistency

---
*All issues linked to AnthonyMFuller/Dungnz repository.*
