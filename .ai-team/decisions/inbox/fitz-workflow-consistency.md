# Decision: All workflows must follow restore → build (--no-restore) → test (--no-build) pattern

**Raised by:** Fitz  
**Date:** 2026-03  
**Context:** Issue #1231 — CodeQL workflow was missing explicit restore step

## Decision
All GitHub Actions workflows that build .NET code must follow this explicit step order:
1. Cache NuGet packages (`actions/cache@v4`, key on `hashFiles('**/*.csproj')`)
2. Restore dependencies (`dotnet restore <solution>`)
3. Build with `--no-restore`
4. Test with `--no-build` (where applicable)

## Rationale
Relying on `dotnet build`'s implicit restore is brittle. If `--no-restore` is ever added (common performance optimisation), the build fails silently. Explicit restore also makes caching effective — the restore step benefits from the NuGet cache, whereas implicit restore inside build may not.

## Affected workflows (all now compliant)
- squad-ci.yml ✅
- squad-release.yml ✅  
- codeql.yml ✅ (fixed #1231)

## Note on local scripts
Local scripts (e.g., `scripts/coverage.sh`) must stay in sync with CI thresholds. CI is authoritative; scripts mirror it (#1228).
