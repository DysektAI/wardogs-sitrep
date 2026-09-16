# VALIDATION

## Decisions
- Stack: C#, .NET 10.0.303, WPF, win-x64. SDK pinned in global.json.
- OCR: Tesseract 5.2.0 + tessdata_fast eng (SHA256 7D4322BD2A7749724879683FC3912CB542F19906C83BCC1A52132556427170B2, retrieved 2026-09-16).
- Data: Apollyon L81 (132–684 m, coverage 80–697 m), linear interpolation, no extrapolation, uncorrected table. See src/Sitrep.Core/Data/PROVENANCE.md.
- Live mode starts disabled. No memory/input synthesis. Foreground guard defaults to disabled until title match configured.

## Executed checks
- Synthetic: `dotnet test` 72/72 pass (parser, math fixture 233.362379/3.193449 tol 1e-6, table exact/interior/limits, state races, ROI).
- Supplied-image: `--self-test` engine ok, synthetic parsed True. `--diagnose-image MapCords.png --crop 900,640,300,180` → x101.53 y107.77 conf 0.91. `--diagnose-image MapPing.png --crop 900,300,300,160` → x101.66 y110.10. Full-image without crop correctly rejects (background grid). 5 synthetic negatives (blank, grid-only, popup-only, cropped, conflict) all reject, zero wrong accepts.
- Packaged Windows: `scripts/publish.ps1` → out/Sitrep-win-x64 + .zip (72 MB). Published exe runs from C:/, self-test and diagnose A pass. Build 0 errors.
- Calculation: fixture range 233.362379 m, bearing 3.193449°, elevation ~755.637621 MIL (229→760, 239→750 linear).
- Overlay rework (per user): 50% transparent (#80000000), top-right default with OverlayLeft/OverlayTop config override, Topmost, never auto-hides — shows status text (DISABLED/WINDOW LOST) instead of hiding so no stale solution looks actionable. Rebuilt, 72/72 pass, self-test + both fixture diagnoses re-verified, republished.
- Cleanup: removed prior agent's dead helpers (scripts/analyze-fixture.ps1, fixture-column-analysis.ps1, fixture-crop-analysis.ps1) and superseded fixture-crops/ (positive coverage now via MapCords/MapPing + documented crops). Kept user screenshots, BUILD_SPEC.md, REVIEW.md, AGENT_PROMPT.md.
- Rename to SITREP: solution/projects/namespaces/assembly now Sitrep.* (`Sitrep.exe`, `%LocalAppData%/Sitrep/`). Rebuilt clean (stale apphost discarded), 72/72 pass, self-test + both fixture diagnoses re-verified, republished. Historical spec docs still reference old paths.

## Blockers / pending
- Real-game validation (Gate E) not run: cursor-label anchoring at zooms/UI scales/edges, MMB timing, F7 fallback, pan/zoom, rapid targets, F9/F10 during OCR, alt-tab, game exit, p95 ≤500 ms, idle CPU, permission confirmation in process per user.
- CI workflow created (.github/workflows/ci.yml) but not run here.
- Overlay focus/click-through smoke needs in-game check. Desktop test mode exists but untested in game.

## Next user test
1. Confirm publisher permission, set config title match, enable live, F8 origin then MMB target on known point, compare overlay vs visible labels.
