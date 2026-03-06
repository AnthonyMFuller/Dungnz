### 2026-03-06: Retrospective action items
**By:** Coulson (facilitating), Hill, Romanoff  
**What:** Process and architectural changes from team retrospective ceremony

**Context:** Team retrospective identified recurring structural issues: same bugs re-occurring across handlers, display layer untestable, P1 gameplay bugs unfixed while shipping UI polish. All three members independently flagged display layer as primary pain point.

## Action Items (Priority Order)

### 1. Gate new features on P1 gameplay bug fixes
**Owner:** Coulson  
**What:** Create "P1 Gameplay Debt" milestone in GitHub. All P1 gameplay bugs (SetBonusManager dead code, boss loot scaling, HP clamping) must close before next feature branch merges.  
**Why:** Hill's recommendation: "Stop shipping UI polish over a foundation with holes." These bugs are broken *now*, not "someday" issues.  
**Timeline:** This sprint

### 2. Enforce ShowRoom() restoration at architectural level
**Owner:** Hill  
**What:** Design and propose enforcement mechanism for ShowRoom() calls on command handler exit paths. Two options: (a) CommandContext contract with interface/abstract base class enforcement, or (b) smoke tests verifying display state after cancel.  
**Why:** "ShowRoom() not called on cancel" has occurred 8+ times across handlers. Social convention has failed. Need structural enforcement.  
**Timeline:** Design proposal by end of sprint

### 3. Add cancel-path tests to every command handler
**Owner:** Romanoff  
**What:** Add cancel-path test template to CommandHandlerSmokeTests.cs. Pattern: "inventory + cancel input → assert ShowRoomCalled == true".  
**Why:** Romanoff should not be manually auditing handlers and writing tables. Test suite should catch this automatically.  
**Timeline:** Template added this sprint, retroactive tests for existing handlers next sprint

### 4. Refactor budget for SpectreLayoutDisplayService
**Owner:** Coulson  
**What:** Draft refactor proposal to extract panel-state management from input handling. Not a full rewrite — small structural split to stop regression cycle. Options: (a) split Input.cs from State.cs, (b) introduce PanelStateManager.  
**Why:** SpectreLayoutDisplayService is 2,600+ LOC doing input, layout, Live rendering, and panel state. Menu cancel can corrupt panel state because all concerns tangled. Hill: "Not as bad as old GameLoop, but heading that way."  
**Timeline:** Proposal drafted by end of sprint, implementation next sprint

### 5. Add integration tests for display layer
**Owner:** Romanoff + Hill (pair)  
**What:** Design integration-style smoke tests for command → display → state cycle in headless mode. Spectre rendering excluded, but state transitions (content panel updated, stats refreshed, map updated) should be verifiable.  
**Why:** SpectreLayoutDisplayService marked `[ExcludeFromCodeCoverage]` — that's where half the bugs live. Need automated coverage for state transitions even if rendering is excluded.  
**Timeline:** Design session 1 sprint, not blocking current work

## Feature Backlog (Suggested Implementation Order)

Based on team suggestions, recommend implementing in sequence:
1. **Persistent event log** (Romanoff's suggestion) — 1-2 days, fixes immediate UX gap
2. **Persistent dungeon floors** (Hill's suggestion) — 3-4 days, unlocks exploration loop
3. **Combat encounter variety** (Coulson's suggestion) — 4-5 days, makes endgame engaging

Total: 8-11 days for all three if done sequentially.

## Process Changes

- **Retrospectives quarterly** — This ceremony format worked (grounded, honest, no fluff). Repeat every quarter.
- **FakeDisplayService enforcement** — Romanoff identified anti-pattern: fake is too permissive. Make it enforce contracts (track call order, verify ShowRoom after SetContent). Infrastructure work but pays off long-term.

---

**Sign-off:** All action items documented. Owners assigned. Timeline set. Retrospective ceremony complete.
