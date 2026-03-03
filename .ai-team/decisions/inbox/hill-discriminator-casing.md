# Decision: JsonDerivedType Discriminator Casing Convention

**Author:** Hill
**Date:** 2026-03-03
**Related Issue:** #873
**Related PR:** #891

## Decision

All `[JsonDerivedType]` discriminator strings must use **all-lowercase** with no separators.

**Rule:** Take the class name, lowercase every character, concatenate. No underscores, no hyphens, no PascalCase.

Examples:
- `DarkKnight` → `"darkknight"`
- `GoblinShaman` → `"goblinshaman"`
- `VampireLord` → `"vampirelord"`

## Rationale

`System.Text.Json` polymorphic deserialization is **case-sensitive** by default. Mixed casing (some PascalCase, some lowercase) causes silent deserialization failures — wrong-type or null results with no exception thrown. This creates hard-to-debug save corruption.

All-lowercase is already used by the majority of our enemy discriminators (31 out of 41 before this fix). Standardizing eliminates the inconsistency.

## Backward Compatibility Impact

Save files written with the old PascalCase discriminators ("Goblin", "Skeleton", "Troll", "DarkKnight", "Mimic", "StoneGolem", "VampireLord", "Wraith", "DungeonBoss", "DungeonBoss") will **not** deserialize correctly after this change.

**Decision:** No migration tooling. Saves are ephemeral in the current dev phase. If this becomes customer-facing before a migration is implemented, a JsonConverter shim with a case-insensitive fallback should be added.

## Applies To

- `Models/Enemy.cs` — `Enemy` base class `[JsonDerivedType]` attributes
- Any future base class using `[JsonPolymorphic]` + `[JsonDerivedType]`

## Action Required

- Romanoff: Add a round-trip serialization test per acceptance criteria in #873 that verifies each enemy subtype serializes and deserializes correctly with the new discriminators.
