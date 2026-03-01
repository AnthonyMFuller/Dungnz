### 2026-03-01: GitHub issues created for text alignment bugs
**By:** Coulson  
**What:** Created 6 GitHub issues for DisplayService.cs alignment bugs  
**Why:** Boss requested issues be created before fixes are implemented

#### Issues Created

| Issue # | Title | File | Line |
|---------|-------|------|------|
| #667 | VisibleLength helper doesn't account for wide BMP chars | Display/DisplayService.cs | 1438-1439 |
| #668 | ShowItemDetail title padding uses raw .Length instead of VisualWidth | Display/DisplayService.cs | 399 |
| #666 | ShowShop item row icon padding uses raw .Length instead of VisualWidth | Display/DisplayService.cs | 475 |
| #663 | ShowEnemyDetail elite tag row is 1 column short | Display/DisplayService.cs | 1341 |
| #664 | ShowVictory banner off by 1 column (too narrow) | Display/DisplayService.cs | 1353 |
| #665 | ShowGameOver banner 2 columns too wide (☠ double-width not accounted) | Display/DisplayService.cs | 1375 |

#### Dependency Chain
- **#667 (VisibleLength)** is foundational: #668 and #666 depend on VisualWidth working correctly
- **#663, #664, #665** are independent fixes

#### Next Steps
Assign to team members for implementation.
