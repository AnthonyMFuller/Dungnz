# Session: 2026-02-24 - Workflow Round 2

**Requested by:** Anthony

## Summary
Fitz implemented second round of GitHub Actions reductions:

- **squad-release.yml**: removed dotnet build/test steps (now just tag + gh release)
- **squad-preview.yml**: deleted; .ai-team/ check folded into squad-ci.yml (preview branch conditional)
- **squad-heartbeat.yml**: converted to workflow_dispatch-only (removed issue/PR event triggers)

**Workflows remaining:** 9 (down from 12)
