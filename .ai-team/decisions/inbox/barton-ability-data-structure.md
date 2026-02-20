### 2026-02-20: Ability System Architecture

**By:** Barton
**What:** Combat abilities use in-memory data structures (List<Ability>) rather than JSON config files
**Why:** Simpler initial implementation for 4 fixed abilities. Hardcoding in AbilityManager constructor provides type safety and avoids deserialization complexity. If ability count grows significantly (>10) or requires frequent balance tuning by non-developers, consider migrating to JSON config similar to enemy/item stats.
