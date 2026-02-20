### 2026-02-20: GameEvents Event System Architecture
**By:** Coulson
**What:** Instance-based event system with optional subscribers using nullable dependency injection
**Why:** 
- Testability requires instance-based (not static) events for mocking and isolation
- Nullable GameEvents? parameter pattern removes tight coupling — events fire unconditionally, subscribers are optional
- Strongly-typed EventArgs provide compile-time safety and rich context for subscribers
- Firing events AFTER state changes ensures subscribers see consistent game state
- Pattern established: inject shared GameEvents instance into subsystems (CombatEngine, GameLoop) at construction
