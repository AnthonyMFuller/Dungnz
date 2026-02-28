# Session: UI Consistency Fixes
**Date:** 2026-02-28
**Requested by:** Copilot (on behalf of Anthony)

## Summary
Hill investigated and fixed 3 UI consistency bugs in Display/DisplayService.cs

## Issues & PR
- GitHub issues: #597, #598, #599
- Pull request: #600

## Bugs Fixed
1. **Warrior icon inconsistency in class menu**
2. **Rogue indentation**
3. **Card border misalignment**

## Root Cause
⚔ icon length=1 vs emoji length=2 in C# string padding calculations

## Solution
- Removed icons from selection menu labels
- Implemented dynamic icon-length-based padding for cards
