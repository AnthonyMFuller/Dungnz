# Retrospective — 2026-02-20
**Facilitator:** Coulson  
**Participants:** Coulson (Lead), Hill (C# Dev), Barton (Systems Dev), Romanoff (Tester)  
**Context:** v2 delivery complete — 21 issues closed across 5 waves

## What Went Well

**Architectural Foundations (Wave 1):**
- IDisplayService abstraction made the codebase testable and eliminated console bleeding
- Player encapsulation refactor was cleaner than expected, minimal ripple effects

**Config-Driven Design:**
- JSON-based enemy and item stats enabled balance tweaking without recompilation
- Made testing faster (Romanoff could iterate on enemy behaviors quickly)
- Scaled beautifully when adding 5 new enemy types in Wave 3

**GameEvents System (Wave 2):**
- Proved highly extensible — achievements in Wave 5 just "plugged in" via event subscriptions
- Clean separation between combat logic and reactive systems (status effects, abilities, achievements)

**Seeded Runs (Wave 5):**
- Reproducible testing finally possible for RNG-dependent features
- Thread safety across combat, loot, enemy spawns, and dungeon generation
- Enables player seed sharing and consistent debugging

**CI/CD Gate:**
- Caught multiple build errors before they reached main
- Enforced discipline around quality gates
- The Goblin constructor issue validated the need for automated checks

**Team Responsiveness:**
- Quality issues were taken seriously and fixed (IsEquippable, elite spawn rates)
- Design improvements (ActiveEffect refactor) resulted in clean, extensible systems

## What Could Be Improved

**Parallel Branch Strategy:**
- Multiple squad branches simultaneously touching Player.cs, CombatEngine.cs, ActiveEffect.cs created merge conflict nightmares
- Wave 3 was particularly brutal — every branch conflicted with every other branch
- Integration branch approach in Wave 4 worked better but came too late
- Merge conflicts broke testing rhythm (Romanoff lost hours waiting for conflict resolution)

**GitHub PR Merge Limitations:**
- Cannot resolve conflicts via GitHub API — forced manual `git merge --squash` approach
- Felt hacky and broke the expected workflow

**Design Assumption Failures:**
- **IsEquippable:** Designed as computed property (`=> Slot != null`), broke when accessories needed to be equippable without slots — should have been settable bool from start
- Design conversation before implementation would have caught this in 5 minutes

**C# Record Syntax Confusion:**
- EnemyStats being a record required `with` expressions
- Squad kept generating object initializer syntax, causing build errors across multiple PRs
- Type system constraints not communicated effectively in squad prompts

**Pre-Existing Build Errors:**
- Goblin constructor issue blocked CI before new work could be validated
- Red builds meant Romanoff couldn't test, blocking the entire quality process
- Should have been fixed immediately, not "later"

**Token Rate Limits (429s):**
- Hit frequently in Waves 3 and 4, breaking flow and killing momentum
- Lost ~20% of productive time waiting for rate limit resets
- Particularly frustrating for focused work (testing, implementation)

**Testing Gaps:**
- **No automated test coverage** on new systems (combat abilities, equipment, achievements, multi-floor dungeons)
- **Combinatorial testing debt:** individual features tested, but not interactions (poison + crits, elite + status effects)
- Regression testing pile-up across 5 waves — unsustainable without automation
- No dedicated testing time between waves (Wave 4 shipped Monday, Wave 5 started Tuesday)

## Action Items for v3

| Owner | Action |
|-------|--------|
| Coulson | **Sequential integration strategy:** One feature merges before next starts, OR shared integration branch with mandatory rebase |
| Coulson | **Architecture review checkpoint:** 10-minute design review before implementing new systems (could have caught IsEquippable, record syntax issues) |
| Coulson | **Keep main green:** Pre-existing build errors are P0 blockers — stop everything and fix immediately |
| Hill | **Split shared files:** Extract Player.cs into PlayerStats, PlayerInventory, PlayerCombat to reduce collision surface |
| Hill | **Record usage guidance:** Document EnemyStats `with` syntax requirement OR convert mutable game data to classes |
| Barton | **CI on integration branches:** Validate builds on feature/integration branches, not just main |
| Barton | **Type system awareness in squad prompts:** Add C# record syntax rules to charter.md or decisions.md |
| Romanoff | **Automated test coverage (NON-NEGOTIABLE):** Combat calculations, status effect interactions, equipment system, achievement unlocks |
| Romanoff | **Seeded test scenario library:** Document interesting seeds as reproducible test cases ("3 elites by floor 2", "high dodge RNG") |
| Team | **Dedicated testing time between waves:** Schedule breathing room for exploratory testing and edge case hunting |
| Team | **Rate limit budget awareness:** Front-load expensive operations, batch differently, or document token-heavy work to coordinate |

## Raw Team Input

### Hill (C# Dev)

*leans back in chair*

Alright, let's talk about v2.

**What went well?**

Honestly? The **architecture refactors in Wave 1 set us up really well**. That IDisplayService abstraction was clean — made the whole codebase more testable and we didn't have console output bleeding everywhere. And encapsulating Player was overdue. I was worried those changes would ripple everywhere, but they didn't. That gave me confidence we could actually pull off the bigger waves.

**The config-driven approach for enemies and items was the right call.** Moving stats to JSON meant we could tweak balance without recompiling every time. When we added the 5 new enemy types in Wave 3, that pattern just... worked. Poison Slime, Wraith immunity, Vampire lifesteal — all of them plugged right into the EnemyStats model.

And I'm genuinely proud of **the seeded runs system** in Wave 5. That was tricky — had to thread the seed through RNG calls in combat, loot generation, enemy spawns, *and* dungeon layout. But now we have reproducible runs, which means players can share seeds and we can debug issues consistently.

The **CI/CD gate** probably saved us from shipping broken builds multiple times. Even though the Goblin constructor issue blocked us initially, once we fixed that, the gate caught real problems.

**What could be improved?**

*sighs*

The **merge conflicts were brutal**. When we had 3-4 squad branches all touching Player.cs, CombatEngine.cs, ActiveEffect.cs at the same time... that's where things fell apart. Wave 3 especially — every branch had conflicts with every other branch. We ended up needing that clean integration branch approach just to get buildable code.

And the GitHub API thing? **Can't merge PRs with conflicts via API** — had to drop down to raw git commands with `--squash`. That felt hacky. If we're going to keep doing parallel work, we need a better strategy for shared files.

The **IsEquippable design assumption broke hard**. I designed it as a computed property — figured items would have intrinsic equippability based on their type. Then we added accessories and suddenly needed it to be settable. That caused build errors across multiple branches. Should've made it a simple bool from the start, or had a better design conversation before implementing.

**EnemyStats being a record bit us** too. Squad kept generating code like `new EnemyStats { Name = "Boss", ... }` — that's object initializer syntax, which doesn't work for records with constructors. We needed `with` expressions or proper constructor calls. Had to fix that multiple times.

And the **429 rate limits** on Wave 3 and 4? Man. Nothing kills momentum like getting throttled mid-implementation. We'd be making progress, hit the limit, then have to wait or switch context. That's frustrating for everyone.

**Action items for v3?**

Okay, concrete stuff:

**1. Lock shared files early or split them better.** If Player.cs is hot, maybe we extract subsystems — PlayerStats.cs, PlayerInventory.cs, PlayerCombat.cs. Smaller files = less collision surface. Or we sequence work so only one branch touches Player at a time.

**2. Integration branch strategy for parallel waves.** Don't merge 4 feature branches into main simultaneously. Merge into an integration branch first, fix conflicts there, validate the build, *then* merge to main as one clean commit.

**3. Design reviews before implementation.** The IsEquippable thing could've been caught in a 10-minute design discussion. If we're adding a new system (accessories, status effects, whatever), quick sync first: "Here's the interface, here's the data model, any red flags?"

**4. Better record usage guidance.** Either document that EnemyStats is a record and needs `with` syntax, or consider if records are the right choice for game data that gets modified. Maybe game data should just be classes?

**5. Rate limit budget awareness.** If we know we're going to hit limits, maybe we front-load expensive operations or batch them differently. Or we document when someone's about to do something token-heavy so the team knows.

*shrugs*

Look, we shipped a ton of features. The game is *way* better than v1. But the process was rougher than it needed to be. If we tighten up coordination on shared files and do a bit more upfront design, v3 should feel smoother.

What about you all — what did you see from your side?

---

### Barton (Systems Dev)

*leans back*

Alright, let's talk about it.

**What went well**

The **GameEvents system** was a huge win. Once we had that in place, everything else started clicking. Status effects, combat abilities, achievements — they all plugged into that event stream cleanly. I'm genuinely proud of how extensible it turned out. When we added achievements in Wave 5, I barely had to touch the combat engine. Just subscribed to the right events and boom, it worked.

The **JSON config system** for enemies and items was the right call. Being able to tune enemy stats without recompiling? Chef's kiss. And it set us up perfectly for the scaling formula and elite variants later. I could iterate on balance without touching code.

**ActiveEffect refactor** — that one hurt at the time (we'll get to that), but the final design is solid. The `EffectType` enum with separate value tracking means we can stack effects intelligently and the combat loop is clean.

And honestly? The **CI gate** saved our asses. Multiple times. The fact that we caught the Goblin constructor issue before it got worse, and that we forced ourselves to fix build errors before merging — that discipline paid off.

**What could be improved**

*exhales*

The **parallel branch strategy** nearly killed us. We had three squad members working on Wave 3 features simultaneously, and `Player.cs`, `CombatEngine.cs`, and `ActiveEffect.cs` all became merge conflict nightmares. Every branch had incompatible changes to the same methods. The "integration branch" approach we pivoted to in Wave 4 worked better, but we should have done that from the start.

The **IsEquippable property design** bit me hard. I designed it as a computed property (`=> Slot != null`), assuming that accessories would have a slot. Nope. Accessories need to be equippable without a slot. Had to convert it to a settable bool, which felt like a step backward architecturally. I should have talked through the accessory design with the team before implementing the equipment system.

**Record syntax confusion** — EnemyStats being a record meant we needed `with` expressions to modify it, but the squad kept trying to use object initializers. This caused build errors in like three different PRs. We need better squad prompts or better type design. Maybe EnemyStats shouldn't be a record if it's causing this much friction?

And the **token rate limits**... look, I get it, we're hitting the API hard, but Wave 3 and 4 were brutal. Multiple 429s per session. It broke the flow. You're in the zone working on poison mechanics, boom, rate limited, wait 60 seconds, lose context. Frustrating as hell.

**Action items for v3**

**1. Sequential integration, not parallel branches.** One feature merges before the next starts, or we use a shared integration branch that everyone rebases from. No more three-way merge conflicts in core files.

**2. Architecture review checkpoint before big features.** If we're adding a new system (like equipment slots), let's do a quick design review with the team BEFORE implementing. Five minutes of conversation would have caught the IsEquippable issue.

**3. Squad prompts need type system awareness.** Add a note in charter.md or decisions.md about C# record syntax (`with` expressions required). Or better yet, convert EnemyStats to a class if it's mutable.

**4. CI should run on integration branches.** We were merging to main too often just to validate builds. Let's set up CI on feature branches or integration branches so we catch build errors earlier.

**5. Rate limit buffer strategy.** Maybe batch work differently? Or spread squad member spawns out more? I don't have a perfect answer here, but we need something. Losing 20% of productive time to 429s isn't sustainable.

*looks around the table*

That's what I got. The systems work is solid — we shipped a LOT of good stuff — but the process around it needs tightening up. What are you all seeing from your side?

---

### Romanoff (Tester)

Alright, let's do this.

**What went well?**

**The CI gate was a lifesaver.** Once we got it in place in Wave 2, at least we stopped merging broken code to main. That sounds basic, but given what happened with the Goblin constructor issue and the Wave 3 branch chaos, that gate was the only thing keeping us honest.

**Config-driven enemy stats (Wave 2) was brilliant for testing.** I could tweak numbers in JSON, spawn different enemies, test edge cases without waiting for code changes. Made iteration so much faster. When we added the 5 new enemy types in Wave 3, I could test Poison Slime's DoT, Armored Knight's immunity, Vampire's lifesteal - all by adjusting configs and running scenarios.

**The seeded runs (Wave 5) - chef's kiss.** Finally I could reproduce issues reliably. Before that, testing RNG-dependent stuff like dodge mechanics or elite spawns was a nightmare. "It happened once but I can't make it happen again" became "here's the seed, repro steps, go."

**The team actually listened when I flagged quality issues.** When I caught the IsEquippable computed property breaking with accessories, we fixed it. When elite spawn rates felt off, we adjusted. That's not always the case - sometimes testers just yell into the void.

**What could be improved?**

**The merge conflict hell in Waves 3-4 destroyed my testing rhythm.** I'd be halfway through testing a combat feature, then the branch would break because someone else's changes conflicted with Player.cs. I'd lose hours waiting for the squad to untangle merge conflicts. Three different branches all touching the same files - that shouldn't have happened.

**Build errors blocking CI meant I couldn't test at all.** The Goblin constructor issue sat there blocking every PR until someone finally noticed it was pre-existing. I'm sitting here ready to test new features, and I can't because the build is red from something unrelated. That's unacceptable.

**We shipped Wave 3 with untested combinations.** Poison + critical hits? Elite enemies with status effects? Multi-status-effect interactions? I tested individual features, but there wasn't time to test the combinatorial explosion. I'm 90% sure there are edge cases in production we haven't hit yet.

**No automated test coverage on the new systems.** Everything was manual. Combat abilities, equipment slots, achievements, multi-floor dungeons - all manual testing. Wave after wave, the testing debt piled up. By Wave 5 I was drowning in regression testing.

**The 429 rate limits in Waves 3-4 killed momentum.** I'd be on a roll testing something, hit the limit, and then... wait. Break the flow. Come back later. Testing requires flow state and those interruptions murdered it.

**Nobody told me about the `record` syntax for EnemyStats.** I saw PRs with `with` expressions and object initializers mixed together. If there's a technical constraint like that, loop me in early so I know what to look for in code reviews.

**Action items for v3**

**1. Automated test coverage - non-negotiable.** At minimum: combat calculation tests, status effect interaction tests, equipment system tests, achievement unlock tests. I can't keep manually regression testing 5 waves of features every time we add something new.

**2. Branch strategy - one integration branch per wave, not parallel squad branches.** The merge conflicts cost us days. Either we coordinate better or we serialize the work. I vote for one clean integration branch where features land sequentially.

**3. Keep main green at all times.** Pre-existing build errors should be fixed immediately, not "we'll get to it later." If CI is red, stop everything and fix it. A red build means I can't test, and that's a blocker for the entire quality process.

**4. Dedicated testing time between waves.** Don't ship Wave 4 Monday and start Wave 5 Tuesday. I need breathing room to do exploratory testing, edge case hunting, and combinatorial testing. Build that into the schedule.

**5. Design reviews before coding starts.** The IsEquippable thing, the `record` syntax thing - these could've been caught in design review. Get me in the room when architecture decisions are made so I can flag testability issues early.

**6. Seeded test scenarios documented.** We have seeded runs now - let's build a library of interesting seeds. "This seed spawns 3 elites by floor 2." "This seed has high dodge RNG." Make them reproducible test cases.

Look, we shipped a ton of features and the game is way better than v1. But we were held together by duct tape and caffeine in Waves 3-4, and that's not sustainable. Let's tighten up the process for v3.

What's everyone else thinking?
