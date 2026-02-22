# RunStats Type — Confirmed Exists

**Date:** 2025-01-20  
**Author:** Barton  
**Status:** Resolved

---

## Summary

The UI/UX shared infrastructure spec (`.ai-team/plans/uiux-shared-infra-spec.md`) requires a `RunStats` type for the `ShowVictory` and `ShowGameOver` display methods.

**CONFIRMED:** `RunStats` already exists in the codebase.

---

## Location

**File:** `Systems/RunStats.cs`  
**Fully-qualified type:** `Dungnz.Systems.RunStats`  
**Type:** Class (not record)

---

## Current Shape

```csharp
public class RunStats
{
    public int FloorsVisited { get; set; }
    public int TurnsTaken { get; set; }
    public int EnemiesDefeated { get; set; }
    public int DamageDealt { get; set; }
    public int DamageTaken { get; set; }
    public int GoldCollected { get; set; }
    public int ItemsFound { get; set; }
    public int FinalLevel { get; set; }
    public bool Won { get; set; }
    public TimeSpan TimeElapsed { get; set; }
}
```

---

## Usage Sites

Already integrated:
- `Engine/GameLoop.cs` — instantiates and tracks RunStats throughout gameplay
- `Engine/CombatEngine.cs` — receives `RunStats?` parameter in `RunCombat()`
- `Systems/AchievementSystem.cs` — uses RunStats for achievement evaluation

---

## Recommendation for Hill

Hill can safely reference `Dungnz.Systems.RunStats` in the `ShowVictory` and `ShowGameOver` display method implementations. No new type needs to be created.

The existing class provides all necessary fields for end-of-run display:
- Kills → `EnemiesDefeated`
- Gold earned → `GoldCollected`
- Items found → `ItemsFound`
- Floors cleared → `FloorsVisited`
- Play time → `TimeElapsed`
- Damage stats → `DamageDealt`, `DamageTaken`

---

## Action Needed

✅ **None.** RunStats exists and is ready to use. Hill can proceed with Phase 0 implementation without waiting on Barton.
