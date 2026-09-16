# SITREP — WARDOGS companion app (mortar solver trial)

Windows/C# helper: F8 sets mortar origin, middle-click/F7 captures target from visible map coordinates, shows range, bearing, and source-backed L81 MIL (uncorrected table).

## Controls
- F8: capture origin. F7 or middle-click: capture target. F9: clear. F10: disable live. Live starts disabled; use the Enable live button to start (hotkeys are only polled while live capture is on).
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

## Testing without the game (already verified here)
- `Sitrep.exe --self-test` — dependency smoke: engine loads, synthetic label parses.
- `Sitrep.exe --diagnose-image MapCords.png --crop 900,640,300,180` — must print x101.53 y107.77, exit 0.
- `Sitrep.exe --diagnose-image MapPing.png --crop 900,300,300,160` — must print x101.66 y110.10, exit 0.
- Full-image or garbage input must reject with exit 2, never a guessed coordinate.

## Testing in game
1. Confirm publisher permission first.
2. Open the game in borderless/windowed (SDR), open its map.
3. In config.json set ForegroundTitleContains to match the game window title, start the app, press Enable (or F10).
4. Stand at the mortar, press F8 (overlay shows ORIGIN SET).
5. Hover a target on the map, middle-click (or F7). Overlay should show the same numbers as the map labels plus range/bearing/MIL.
6. Try F9 (clears), F10 twice (disable/enable), alt-tab out and back (overlay shows WINDOW LOST, old solution gone).

## Diagnostics
- `Sitrep.exe --self-test` (dependency smoke, not accuracy proof).
- `Sitrep.exe --diagnose-image <png> --crop x,y,w,h --report out.json` (uses live pipeline; exit 2 on OCR reject).

## Limits
- L81 only, 132–684 m, linear table, no terrain correction. Outside limits shows OUT OF RANGE. Missing table shows TABLE UNVERIFIED. Failures never show plausible MIL.
- Local-only processing, no network/telemetry. Publisher permission unresolved — get explicit approval before in-game use. No memory read, injection, or input synthesis.
