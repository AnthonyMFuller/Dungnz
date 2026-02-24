# Retrospective — 2026-02-24
**Facilitator:** Coulson  
**Participants:** Hill, Barton, Romanoff, Fury, Fitz  
**Requested by:** Anthony

## What Went Well

**Architecture delivered on its promises.** Hill noted that the sell feature shipped with clean separation of concerns — display, logic, and data each in the right place. The `IDisplayService` contract discipline held, `ItemConfig` as single source of truth proved robust, and the `FakeDisplayService` pattern prevented test-breaking surprises. Barton echoed this: adding `SellPrice` as a JSON field meant zero hardcoded values scattered across merchant logic. Data-driven design worked exactly as intended.

**Narration system continues to scale.** Fury reported that `NarrationService.Pick()` and the per-system static classes (`MerchantNarration`, etc.) absorbed sell narration with zero structural changes. Pure content work, no code friction. The merchant voice stayed tonally consistent across all interaction types, and pool-size discipline prevented player repetition fatigue.

**Test-first culture is internalizing.** Romanoff: 11 tests shipped *with* the feature, not after it. 442/442 green at merge, no regressions. The suite held across merchant logic, display formatting, and 53-item data updates. Hill and Barton are now writing tests as part of feature definition, not cleanup work.

**CI/CD held up under load.** Fitz reported that the `squad-release.yml` Node.js bug (PR #351) is finally fixed, and `squad-ci.yml` ran clean on PR #357. The PR gate did its job — no regressions slipped through.

**Feasibility research pays forward.** Barton called out that the ASCII art feasibility research from his last cycle identified a zero-breaking-change path with clear insertion points. That's the kind of planning artifact that prevents mid-sprint rework.

---

## What Could Be Improved

**The duplicate PR (#356/#355) was a preventable process failure.** All five agents surfaced this. Root cause: PR #357 didn't include `Closes #355`, leaving the issue orphaned and open. The Copilot coding agent legitimately picked it up as unclaimed work. No one owned or assigned the issue before work started. Romanoff: "Five seconds of discipline prevents duplicate work." Barton: "This cost real time." Fury: "If our issues had clear owner tags, an autonomous agent would have had a harder time deciding to own the whole thing." Fitz: "This is a workflow gap, not a code failure."

**GameLoop.cs is accumulating command-handling responsibility.** Hill: "`HandleSell()` lives there, `HandleCraft()` probably will too, `HandleCombat()` is there. The game loop should be a router, not a handler." Barton agreed: "The merchant logic (buy, sell, inventory, banter) arguably belongs in a `MerchantSystem` or `MerchantService`." Romanoff flagged the testability gap: "`SELL` string routing from user input to `HandleSell()` lives in untestable top-level script code. Command dispatch belongs in a class."

**ItemConfig.cs will not scale.** Hill: "53 items, every field inline. Adding `SellPrice` was mechanical but it's a signal: every new item attribute means touching every item entry." We already have `MerchantInventoryConfig` and `CraftingRecipesConfig` as JSON — items should follow the same pattern.

**SellPrice has no documented formula.** Hill: "The 53 values are inconsistent, and future items will be guesses. We need a pricing convention, even a simple one." Fury: "A quick content/economy pass to make sure rare items *feel* valuable when sold would strengthen player feedback loops without touching code."

**Sell narration pools are thin relative to importance.** Fury: "`AfterSale` and `NoSell` are functional but thin — maybe 5 lines each. Buy narration got more creative passes. Sell is a *new* player-facing loop and deserved the same depth." No content brief reached Fury early; she was reactive rather than proactive.

**CI workflows are inconsistent.** Fitz: "`ci.yml` and `squad-ci.yml` do overlapping but inconsistent work. `ci.yml` runs coverage and enforces XML doc. `squad-ci.yml` runs tests but no coverage, no doc enforcement. Both trigger on PRs to `main`. A PR author gets different signals from the same codebase."

**No warning when selling equipped items.** Barton: "When you sell a weapon that's equipped, the player gets no warning. That's a usability gap."

---

## Action Items

| Owner | Action | Priority |
|-------|--------|----------|
| Fitz | Add PR description linter workflow — validate that PRs include `Closes #NNN` or `Fixes #NNN`. Fail with clear message if missing. | P0 |
| Romanoff | Establish issue-claiming protocol: Before any agent starts a feature, they must comment on the GitHub issue and include `Closes #N` in their PR. Gate PRs missing the reference. | P0 |
| Fury | Add `content: fury` labels to GitHub issues touching flavor text. Signals to humans and agents that this issue is not up for grabs. | P0 |
| Hill | Spike: Migrate `ItemConfig.cs` item definitions to JSON data file, loaded at startup. `MerchantInventoryConfig.cs` is the template. Reduces per-attribute change blast radius. | P1 |
| Hill / Barton | Extract command handlers from `GameLoop.cs`. Commands like SELL, CRAFT, SHOP, USE SHRINE each deserve their own handler method or object, routed from the loop. | P1 |
| Romanoff | Extract command dispatch from `Program.cs` into a `CommandRouter` class with injected `IDisplayService`. Write router tests once the class exists. | P1 |
| Barton | Spike a `MerchantSystem` class that encapsulates buy/sell/banter/inventory. Propose to Coulson before the next feature that touches merchant logic. | P1 |
| Coulson / Barton / Fury | Schedule sell-price economy review pass. Ensure item sell values reinforce item tier perception (Floor 4 drop should feel meaningfully better to sell than Floor 1 trash). | P1 |
| Fury | Expand `AfterSale` and `NoSell` pools to 8–10 lines each. Mix in merchant-specific personality variants. | P2 |
| Fury | Draft a Narration Brief template: call sites, player states, tone notes. Any teammate fills this out when creating a content-touching feature. | P2 |
| Fitz | Unify `ci.yml` and `squad-ci.yml` — one canonical CI workflow for PRs. Keep coverage threshold and XML doc enforcement. | P1 |
| Fitz | Fix release versioning strategy — replace date-only tag with `v{date}-{short-sha}` to guarantee uniqueness per commit. | P1 |
| Fitz | Add NuGet cache to all workflows — one `actions/cache` step for `~/.nuget/packages`. | P2 |
| Barton | Add equip-protection warning to sell flow: warn player before confirming a sell if item is currently equipped. | P2 |
| Romanoff | Add minimum pool-size assertions to narration tests: `pool.Count >= 3` for every banter/narration pool. | P2 |
| Barton | Green-light ASCII art implementation. Feasibility research complete, data layer path clear. Requesting Coulson sign-off to proceed. | P1 |

---

## Raw Notes

### Hill

**The sell feature shipped clean.** `SellPrice` on all 53 items, `HandleSell()` in the game loop, `ShowSellMenu()` in the display layer — each concern landed in the right file. No bleed between display and logic. That's the architecture working as intended.

**The display layer held up under load.** After the Phase 1–4 UX sprint, `DisplayService.cs` absorbed ShowSellMenu without drama. The `ColorizeItemName` + ANSI-safe padding pattern we locked in during Phase 2 meant I didn't have to reinvent alignment logic — I just used it. That convention is paying dividends.

**ItemConfig as a single source of truth proved robust.** Adding `SellPrice` to 53 items was tedious, but the fact that all item definitions live in one place (`ItemConfig.cs`) meant there was exactly one place to touch. No hunting across files. Serializable, auditable, done.

**IDisplayService contract discipline stayed clean.** `ShowSellMenu` went on the interface first, stub added to `FakeDisplayService`, then implemented. No test-breaking surprises. The test/fake pattern we've been enforcing across the team is genuinely good.

**The duplicate PR (#356) situation points to a real process gap.** If a GitHub Issue exists for work we're already doing, the PR description needs `Closes #N` — full stop. This should be a checklist item before any PR is raised, not an afterthought. The fact that a Copilot coding agent picked up Issue #355 and started parallel work while we were mid-implementation is a race condition in our own board. We created the collision.

**`ItemConfig.cs` is going to become unmanageable.** 53 items, every field inline. Adding `SellPrice` was mechanical but it's a signal: every new item attribute means touching every item entry. We should move toward item data loading from JSON (we already have `MerchantInventoryConfig` and `CraftingRecipesConfig` as JSON — items should follow the same pattern). This is a refactor I should own.

**`GameLoop.cs` is accumulating responsibilities.** `HandleSell()` lives there, `HandleCraft()` probably will too, `HandleCombat()` is there. The game loop should be a router, not a handler. Command handling should be extracted — a `CommandHandler` or thin command objects. This is starting to smell.

**No agreed weight for `SellPrice`.** The value for each item was a judgment call. There's no documented formula (e.g., `SellPrice = BuyPrice * 0.4`). That means the 53 values are inconsistent, and future items will be guesses. We need a pricing convention, even a simple one.

1. **Add "Closes #N" to PR template** — Create or update `.github/pull_request_template.md` to include a "Related Issues" section. Enforce it in team process.

2. **Spike: Items from JSON** — I should open a task to migrate `ItemConfig.cs` item definitions to a JSON data file, loaded at startup. `MerchantInventoryConfig.cs` is the template. This reduces per-attribute change blast radius from 53-line edits to one schema change.

3. **Extract command handlers from `GameLoop`** — Open a refactor task. Commands like SELL, CRAFT, SHOP, USE SHRINE each deserve their own handler method or object, routed from the loop rather than implemented inline.

4. **Document SellPrice formula** — Add a brief comment block at the top of the sell price section in `ItemConfig.cs` (or in the README data conventions section) defining the intended ratio. Prevents future drift.

5. **Issue hygiene rule** — Before starting any feature, search open issues first. If the work maps to an open issue, reference it immediately. If no issue exists, create one before opening the PR branch.

### Barton

**SellPrice was the right call, and it landed cleanly.** Adding a `SellPrice` field to all 53 items in `item-stats.json` is exactly the kind of data-driven design I've been pushing for since Phase 2. No hardcoded values in C#, no magic numbers scattered across merchant logic — just a field in the data layer that any system can read. The pattern held up well.

**The merchant narration pools for selling were a natural extension.** We already had banter pools for buying; adding sell-specific narration followed the same pattern without friction. The system composition story here is clean: loot tables, merchant inventory, banter pools, sell prices — all JSON-driven. That was the vision from v2 planning and it delivered.

**442 tests passing at ship is a strong number.** Romanoff's 11 new tests for the sell flow gave me confidence that the `HandleSell()` and `ShowSellMenu()` paths were exercised properly. Having that coverage before merge meant I wasn't shipping blind.

**The ASCII art feasibility research (from my last cycle) paid off as a planning artifact.** Zero-breaking-change path identified, clear insertion point documented, data-driven approach validated. That's the kind of groundwork that prevents rework mid-sprint.

**The duplicate PR (#356) situation was a process failure, not a code failure — but it still cost time.** The root cause was that our GitHub issue (#355) was left open and unassigned while we were actively implementing it. The Copilot agent had no signal that a human was already on it. We also didn't put `Closes #355` in our PR description, which is just discipline. Neither of these is a systems problem, but they're friction I felt.

**`HandleSell()` living in `GameLoop.cs` concerns me.** The game loop is already one of the heavier files. As sell logic grows — tiered pricing, haggling, bulk sell — it'll accrete more there. The merchant logic (buy, sell, inventory, banter) arguably belongs in a `MerchantSystem` or `MerchantService` class that GameLoop simply calls into. I didn't push for this refactor now because the feature scope didn't warrant it, but we're accumulating debt.

**No sell-side item comparison feedback.** When you sell a weapon that's equipped, the player gets no warning. That's a usability gap in my domain (inventory UX). Romanoff may have flagged this in tests, but the feature shipped without it.

1. **Add `Closes #<issue>` to every PR description, always.** Make it a merge-blocker checklist item. Five seconds of discipline prevents duplicate work.

2. **Claim GitHub issues immediately when picking up a feature.** If an issue exists, assign it to the active agent/dev at task start — not at PR time. This is the signal that prevents double-work.

3. **Spike a `MerchantSystem` class** that encapsulates buy/sell/banter/inventory. Propose to Coulson before the next feature that touches merchant logic. Now is cheaper than after haggling or bulk-sell lands.

4. **Add equip-protection warning to sell flow.** Before confirming a sell, check if the item is currently equipped and warn the player. Small delta, high UX value, clean to test.

5. **Green-light ASCII art.** The feasibility research is done. Data layer path is clear. Content scope is bounded (~70 lines of art). This is the highest-impact visual feature I can deliver in one sprint without touching other systems. Requesting Coulson sign-off to proceed.

### Romanoff

**11 tests shipped with the feature, not after it.** The sell-item feature arrived with `HandleSell`, `ShowSellMenu`, and narration pools already covered. That's the pattern I've been pushing for — tests as part of the feature definition, not a cleanup task afterward. Hill and Barton are internalizing this.

**442/442 green, no regressions.** Across a feature that touched merchant logic, display formatting, item data for all 53 items, and a new command handler — nothing broke that wasn't caught before merge. The suite held.

**`FakeDisplayService` continues to pay dividends.** The sell tests could lean on `RawCombatMessages` and `AllOutput` patterns we established in earlier phases. The test infrastructure investment from Phase 1 is compounding correctly.

**The `SellPrice` coverage was thorough.** Testing zero sell price (unsellable items), selling at correct gold calculation, merchant response strings, and inventory removal after sale — the edge cases were addressed, not just the happy path.

**The duplicate PR (#356 / #355) situation was a process failure that wasted real time.** Root cause: the Copilot coding agent picked up our GitHub issue independently while we were implementing the same feature. The fix — `Closes #355` in the PR body — is a one-line discipline that would have prevented the collision. We didn't have a standing rule that issues must be claimed before work starts.

**`HandleSell()` is tested, but `Program.cs` command dispatch is not.** The actual `"SELL"` string routing from user input to `HandleSell()` lives in untestable top-level script code. If someone renames the command or adds a typo, zero tests catch it. This is the same structural problem I flagged on the intro sequence — command dispatch belongs in a class.

**Sell narration pool tests only verify non-null/non-empty.** We confirmed narration lines are returned, but didn't assert tone, minimum pool size, or that the lines actually vary. A pool of one repeated string would pass. Low-risk now, but as merchant banter grows, a pool-coverage test would be cheap insurance.

1. **Establish issue-claiming protocol:** Before any agent starts a feature, they must comment on the GitHub issue ("Picked up by [agent]") and include `Closes #N` in their PR. Add this to the team process document. Romanoff will gate PRs that are missing the closing reference.

2. **Extract command dispatch from `Program.cs`:** Hill or Barton should move the command routing switch/if-chain into a `CommandRouter` class with injected `IDisplayService`. I'll write the router tests once the class exists — this is the single highest-value testability gap in the codebase right now.

3. **Add minimum pool-size assertions to narration tests:** For every banter/narration pool test, assert `pool.Count >= 3` (or whatever the design minimum is). One line per test. I'll add these retroactively to the sell narration tests in the next available slot.

4. **Add a `[Fact]` that exercises the `SELL` command string end-to-end** once command dispatch is extracted. Gate on Action Item 2 completing first.

### Fury

**The narration architecture held up beautifully.** `NarrationService.Pick()` and the per-system static classes (`MerchantNarration`, `ItemInteractionNarration`, etc.) proved their worth the moment we needed to drop in sell flavor. Adding `AfterSale`, `NoSell`, and the sell-price confirmation lines to `MerchantNarration.cs` required zero structural changes — it was pure content work, exactly as designed. That's a win for the pattern we established in Phase 4.

**Sell narration felt tonally consistent.** The merchant voice across `Greetings`, `AfterBuy`, `AfterSale`, and `NoSell` reads as the same character — terse, mercenary, faintly contemptuous. That consistency came from having a single owner for the voice and a centralized file to write into.

**Pool size discipline paid off.** Having 5+ lines per pool means players won't see the same line every transaction. For a high-repetition interaction like selling items, that matters more than for, say, shrine flavor.

**The sell narration pools are thin relative to importance.** `AfterSale` and `NoSell` are functional but thin — maybe 5 lines each. Buy narration got more creative passes. Sell is a *new* player-facing loop and deserved the same depth. It got written quickly at the end of the feature rather than as a first-class content task.

**No content brief for the sell feature reached me early.** The sell-item feature was scoped and partially implemented before I had a defined content spec. I was reactive rather than proactive. A brief from Coulson/Hill at issue-creation time — "here are the 3 call sites, here are the player states we need flavor for" — would have let me write richer copy instead of fitting words into an already-wired system.

**The duplicate-PR incident had a content dimension nobody named.** Part of why the Copilot agent created PR #356 was that issue #355 didn't have a clear "content owner" tag. The agent saw an unclaimed issue and grabbed it. If our GitHub issues explicitly noted "content: Fury / integration: Hill," an autonomous agent would have had a harder time deciding to own the whole thing.

**SellPrice is data, not narrative, but it affects player perception.** Some of the 53 item sell prices feel arbitrary. A quick content/economy pass to make sure rare items *feel* valuable when sold (and junk feels like junk) would strengthen player feedback loops without touching code.

1. **Expand `AfterSale` and `NoSell` pools to 8–10 lines each.** Mix in merchant-specific personality variants — suspicious, greedy, indifferent. Small lift, high repetition-reduction value.

2. **Draft a Narration Brief template.** One short doc (or GitHub issue section) that any teammate fills out when creating a content-touching feature: call sites, player states, tone notes. Prevents reactive writing.

3. **Add "Content Owner" labels to GitHub issues.** `content: fury` as a tag on any issue touching flavor text. Signals to both humans and agents that this issue is not up for grabs.

4. **Schedule a sell-price economy review pass** with Coulson and Barton. Goal: ensure item sell values reinforce item tier perception (Floor 4 drop should feel meaningfully better to sell than Floor 1 trash).

5. **Update `history.md`** to reflect Phase 4 completion and the sell-item feature, so the next sprint starts from an accurate baseline.

### Fitz

**The known `squad-release.yml` bug got fixed.** PR #351 resolved the `node --test test/*.test.js` issue that's been sitting in my history since day one. That was the biggest open DevOps debt on my plate, and it's done. Release pipeline now correctly runs `dotnet test`.

**CI held up under load.** The sell-item feature landed 442/442 tests passing through a clean merge to master. The `squad-ci.yml` PR gate did its job — no regressions slipped through. That's the pipeline doing exactly what it's supposed to do.

**The duplicate PR/issue incident wasn't a pipeline failure.** CI correctly ran on both PRs independently. The tooling did its part; the process around it was the problem.

**The duplicate PR incident is partly a workflow gap.** The root cause was `Closes #355` missing from PR #357's description. If our PR template enforced a `Closes #` line (or if a workflow linted PR descriptions for linked issues), this would have surfunk before merge. We have `squad-label-enforce.yml` and `squad-triage.yml` doing light governance — but nothing that validates issue linkage. That's a gap.

**`ci.yml` and `squad-ci.yml` are doing overlapping but inconsistent work.** `ci.yml` runs coverage with a 70% line threshold and enforces XML doc. `squad-ci.yml` runs tests but no coverage, no doc enforcement. Both trigger on PRs to `main`. That means depending on which workflow runs first, a PR author gets different signals from the same codebase. The two workflows should be unified or clearly separated by purpose.

**Date-based release tagging is fragile.** `squad-release.yml` generates tags like `v2026.01.15`. If two merges land on the same day, the second one silently skips the release (`exists=true` → no tag, no release). That's a silent no-op that's easy to miss. The team may not even notice a merge didn't produce a release.

**No build caching.** None of the workflows cache the NuGet package restore. On a small project this is fine now, but as the dependency tree grows this will start mattering. Easy win to add before it becomes a pain point.

1. **Add a PR description linter workflow** — validate that PRs targeting `dev` or `main` include a `Closes #NNN` or `Fixes #NNN` reference. Fail with a clear message if missing. This directly prevents the class of process failure we just saw.

2. **Unify `ci.yml` and `squad-ci.yml`** — one canonical CI workflow for PRs, not two. Keep the coverage threshold and XML doc enforcement from `ci.yml`. Retire or repurpose `squad-ci.yml`. Consistent signals for the whole team.

3. **Fix the release versioning strategy** — replace the date-only tag with `v{date}-{short-sha}` (e.g., `v2026.01.15-0287f75`). Guarantees uniqueness per commit, no silent skips on same-day merges.

4. **Add NuGet cache to all workflows** — one `actions/cache` step for `~/.nuget/packages`. Low effort, compounds over time.
