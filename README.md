# SITREP — WARDOGS companion app (mortar solver trial)

Windows/C# helper: F8 sets mortar origin, middle-click/F7 captures target from visible map coordinates, shows range, bearing, and source-backed L81 MIL (uncorrected table).

## Controls
- F8: capture origin. F7 or middle-click: capture target. F9: clear. F10: enable/disable live, including when disabled. Live starts disabled. Capture hotkeys are inert until enabled; held keys are reseeded on enable/focus changes.
- Keep the cursor still through the second snapshot (scheduled after 50 ms); movement rejects with `MOVED—TRY AGAIN`. Clipped ROIs or overlap with any SITREP window reject rather than OCR partial/self-generated labels.
- Control window: live-state pill, firing-solution card (elevation, range, bearing, origin/target), startup warning banner, and Enable / Clear / Settings / Exit buttons.
- Settings window: game window title match, desktop test mode, capture region (ROI), overlay position reset, debug captures. Validated on Save and written to config.json.

## Baseline
- .NET 10, WPF, Windows 11 x64, borderless/windowed, SDR. Other modes unverified.

## Run
1. `pwsh -ExecutionPolicy Bypass -File scripts/setup-model.ps1`
2. `dotnet build Sitrep.slnx -c Release`
3. `src/Sitrep.Desktop/bin/Release/net10.0-windows/win-x64/Sitrep.exe`
- Two windows appear by design: the control window (status, firing solution, buttons) and a semi-transparent always-on-top overlay readout for use over the game. The overlay is click-through so it cannot hold buttons — that is why they are separate. Closing the control window exits everything.
- Exit via control window Exit (kills overlay/workers). Config: %LocalAppData%/Sitrep/config.json — edit it via the Settings window or by hand. Set ForegroundTitleContains to your game window title; empty leaves capture disabled. Overlay position: OverlayLeft/OverlayTop in config (defaults to top-right; Settings can reset it). Debug captures: %LocalAppData%/Sitrep/captures (50 files max).

## Verification and local trial artifact
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1` — public clean-clone gate: checksum-pinned model setup, locked restore, zero-warning Release build, tests, locked self-contained publish, packaged self-test/integration checks, failure-path probes, and all five committed negative images.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 -PrivateFixtures` — additionally requires local `MapCords.png` (crop `900,640,300,180`, expected x101.53 y107.77) and `MapPing.png` (crop `900,300,300,160`, expected x101.66 y110.10). Private screenshots are never uploaded by CI.
- Artifact: `out/Sitrep-win-x64/Sitrep.exe` and `out/Sitrep-win-x64.zip`. Launch from the complete extracted folder; Visual C++ x64 runtime must already be installed. No installer/runtime downloads occur in the app.
- Actual results and remaining display/game checks: [docs/VALIDATION.md](docs/VALIDATION.md). Synthetic OCR success is dependency evidence, not proof of game-font accuracy.

## Testing in game
1. Confirm publisher permission first.
2. Open the game in borderless/windowed (SDR), open its map.
3. In config.json set ForegroundTitleContains to match the game window title, start the app, press Enable (or F10).
4. Stand at the mortar, press F8 (overlay shows ORIGIN SET).
5. Hover a target on the map, middle-click (or F7). Overlay should show the same numbers as the map labels plus range/bearing/MIL.
6. Try F9 (clears), F10 twice (disable/enable), alt-tab out and back (overlay hides while live and unfocused, then shows WINDOW LOST until a fresh target; old solution is gone). Game-window closure clears the origin too.

## Diagnostics
`Sitrep.exe` is a GUI-subsystem process. Do not trust direct PowerShell invocation to wait or set `$LASTEXITCODE`; use the same helper as CI (it redirects output and checks the actual process exit):

```powershell
. ./scripts/common.ps1
$exe = (Resolve-Path 'out/Sitrep-win-x64/Sitrep.exe').Path
Invoke-PackagedCheck $exe @('--self-test')
Invoke-PackagedCheck $exe @('--diagnose-image', (Resolve-Path 'MapCords.png').Path,
  '--crop', '900,640,300,180', '--report', "$PWD/out/origin.json")
```

Self-test is dependency smoke. Image diagnosis shares live OCR; exit 0 means accepted, 2 means OCR/dependency rejection, 1 means invocation/file/error failure. Reports preserve raw recipe text and engine confidence (not a correctness probability). Debug mode stores bounded PNG/JSON records (50 files total) locally; the old unbounded `records.log` is retired.

Missing config uses defaults without writing a file. Invalid/unreadable config is preserved and produces one actionable startup error; repair or rename `%LocalAppData%/Sitrep/config.json`. Settings saves validated values atomically.

## Limits
- L81 only, 132–684 m, linear table, no terrain correction. Outside limits shows OUT OF RANGE. Missing/corrupt bundled model or table prevents startup with one error. The state layer suppresses MIL if a table is unavailable. Failures never show plausible MIL.
- The overlay includes origin, target, and the uncorrected-table marker. No claim of real-game validation or public redistribution clearance: see [third-party notices](THIRD-PARTY-NOTICES.md) and [data provenance](src/Sitrep.Core/Data/PROVENANCE.md).
- Local-only processing, no network/telemetry. Publisher permission unresolved — get explicit approval before in-game use. No memory read, injection, or input synthesis.
