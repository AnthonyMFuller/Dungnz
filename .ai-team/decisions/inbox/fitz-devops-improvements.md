# DevOps Improvements — CI/CD Infrastructure Enhancements

**Date:** 2026-03-01  
**Owner:** Fitz (DevOps)  
**Status:** Implemented (6 PRs open)

## Context

Multiple opportunities identified to improve CI/CD infrastructure, dependency management, developer experience, and security scanning. Implemented as 6 separate PRs to allow independent review and merge.

## Decisions Made

### 1. CI Speed Improvements (PR #759)
**Decision:** Remove redundant XML documentation build step and add NuGet caching.

**Rationale:**
- The second `dotnet build Dungnz.csproj --no-restore` step labeled "Enforce XML documentation" is redundant because `WarningsAsErrors` in the .csproj already enforces XML doc warnings during the first build.
- NuGet package downloads happen on every CI run without caching, wasting ~5-10 seconds per run.

**Implementation:**
- Removed redundant build step from `squad-ci.yml`
- Added `actions/cache@v4` step for `~/.nuget/packages` with key based on `**/*.csproj` hashes
- Cache has restore-keys fallback for partial matches

**Impact:** ~10-15 second reduction per CI run (eliminates duplicate build + speeds up restore).

---

### 2. Dependabot for Automated Dependency Updates (PR #761)
**Decision:** Add Dependabot configuration to automate NuGet and GitHub Actions dependency updates.

**Rationale:**
- Manual dependency management is error-prone and time-consuming
- Security updates should be applied promptly
- GitHub Actions dependencies also need regular updates

**Configuration:**
- **NuGet packages**: Weekly updates (Mondays 9am UTC), max 5 open PRs
- **GitHub Actions**: Monthly updates, max 3 open PRs
- All dependency PRs auto-labeled with "dependencies"

**Why these settings:**
- Weekly NuGet updates catch security patches quickly without overwhelming the team
- Monthly Actions updates balance freshness with stability (Actions change less frequently)
- PR limits prevent dependency update spam

**Impact:** Automated security patches, reduced manual work, standardized update cadence.

---

### 3. EditorConfig for Consistent Formatting (PR #763)
**Decision:** Add `.editorconfig` to enforce consistent C# formatting across all editors.

**Rationale:**
- Team uses different editors (Visual Studio, Rider, VS Code)
- No standardized formatting rules lead to noisy diffs
- EditorConfig is IDE-agnostic and widely supported

**Rules Defined:**
- **Global**: UTF-8, LF line endings, trim trailing whitespace
- **C#**: 4-space indent, brace styles (all on new line), using directives sorting, null-coalescing preferences
- **YAML/JSON**: 2-space indent
- **Markdown**: Preserve trailing whitespace (for double-space line breaks)

**Impact:** Consistent formatting across team members, reduced formatting noise in PRs.

---

### 4. Release Artifacts for Players (PR #765)
**Decision:** Publish self-contained executables as release artifacts.

**Rationale:**
- Current releases have no downloadable files — players must clone and build
- Requiring .NET SDK creates friction for non-developer players
- Self-contained executables bundle the .NET runtime

**Implementation:**
- Publish linux-x64 and win-x64 targets with:
  - `--self-contained true` (includes .NET runtime)
  - `PublishSingleFile=true` (single executable, no DLL sprawl)
  - `PublishReadyToRun=true` (optimized startup time)
- Archive each platform as zip file
- Attach zips to GitHub release

**Why these platforms:**
- linux-x64 and win-x64 cover the majority of desktop users
- macOS omitted for now (can add in future if needed)

**Impact:** Players can download, extract, and run without .NET SDK. Zero-friction distribution.

---

### 5. Dotnet Tool Manifest for Stryker (PR #767)
**Decision:** Pin Stryker.NET version using a dotnet tool manifest.

**Rationale:**
- Current workflow installs Stryker globally without version pinning: `dotnet tool install --global dotnet-stryker`
- This means the Stryker version can change between CI runs, causing inconsistent mutation test results
- No version control tracking of tool dependencies

**Implementation:**
- Created `.config/dotnet-tools.json` manifest pinning Stryker to 4.12.0
- Updated `squad-stryker.yml` to use `dotnet tool restore` instead of global install
- Added tool cache (`~/.dotnet/tools`) keyed on manifest hash
- Changed invocation from `dotnet-stryker` to `dotnet stryker` (local tool)

**Why .config/dotnet-tools.json:**
- Standard location for dotnet tool manifests
- Checked into git for version control
- Allows local developers to `dotnet tool restore` for same tool versions as CI

**Impact:** Reproducible mutation testing, version-controlled tool dependencies, faster CI (tool caching).

---

### 6. CodeQL Static Analysis (PR #769)
**Decision:** Add CodeQL workflow for automated security and code quality analysis.

**Rationale:**
- No automated security vulnerability scanning in place
- CodeQL detects SQL injection, XSS, unsafe deserialization, null pointers, resource leaks, etc.
- Free for public repositories (GitHub Advanced Security)

**Implementation:**
- Created `.github/workflows/codeql.yml`
- Runs on push to master/preview, PRs to master, and weekly schedule (Mondays 4am UTC)
- Uses `security-and-quality` query suite for comprehensive analysis
- Results visible in GitHub Security tab

**Why weekly schedule:**
- Catches vulnerabilities introduced between PR-based scans
- Runs during low-activity time (4am UTC)
- Complements PR-based scanning without excessive CI usage

**Impact:** Automated security vulnerability detection, code quality insights, GitHub Security tab integration.

---

## Alternatives Considered

### CI Speed Improvements
- **Alternative:** Keep redundant build for explicit XML doc enforcement
- **Rejected:** Redundant work that `WarningsAsErrors` already handles

### Dependabot
- **Alternative:** Use Renovate Bot instead
- **Rejected:** Dependabot is GitHub-native, simpler setup, sufficient for this project

### Release Artifacts
- **Alternative:** Framework-dependent deployments (smaller binaries but require .NET SDK)
- **Rejected:** Self-contained is better UX for players (no SDK needed)

### Tool Manifest
- **Alternative:** Continue with global Stryker install
- **Rejected:** Violates reproducibility principle, no version tracking

### CodeQL
- **Alternative:** Use commercial SAST tools (Snyk, SonarQube)
- **Rejected:** CodeQL is free, GitHub-native, good enough for this project

---

## Open Questions

None. All implementations are standard industry practices with clear benefits.

---

## Success Metrics

- **CI Speed:** CI runs complete ~10-15 seconds faster (measured after merge)
- **Dependabot:** Dependency PRs opened within first week of configuration
- **EditorConfig:** No formatting-only diffs in PRs after merge
- **Release Artifacts:** Next release includes downloadable zip files for linux-x64 and win-x64
- **Stryker:** Mutation testing uses pinned version from manifest
- **CodeQL:** Security tab shows scan results within first week

---

## Related PRs

- PR #759: CI speed improvements (squad-ci.yml optimization)
- PR #761: Dependabot configuration
- PR #763: EditorConfig
- PR #765: Release artifacts (squad-release.yml)
- PR #767: Stryker tool manifest
- PR #769: CodeQL workflow
