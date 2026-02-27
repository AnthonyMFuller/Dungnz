### 2026-02-27: Guard-rails implemented
**By:** Hill
**What:** Added `master` to the `pull_request` branch trigger in `.github/workflows/squad-ci.yml`. Previously, `push` to master was covered but PRs *targeting* master were not — meaning a feature branch could compile-fail undetected until it landed.

The CI (`squad-ci.yml`) already runs `dotnet build` + `dotnet test` + coverage threshold enforcement. The gap was that its `pull_request:` trigger listed `[dev, preview, main]` but omitted `master`. One-line fix: added `master` to that list.

**WarningsAsErrors audit:** `Dungnz.csproj` already has `<WarningsAsErrors>CS1591;CS0108;CS0114;CS0169;CS0649;IDE0051;IDE0052</WarningsAsErrors>`. The sell-bug failure was a hard compiler **error** (CS0117 — method not found), not a warning, so it would have been caught by `dotnet build` regardless. The root cause was that the PR's CI run never triggered against master. No csproj changes required; `dotnet build` reports 0 warnings and 0 errors.

**Why:** Prevent compilation errors from reaching master without detection.

**Change made:**
```diff
# .github/workflows/squad-ci.yml
 on:
   pull_request:
-    branches: [dev, preview, main]
+    branches: [dev, preview, main, master]
```
