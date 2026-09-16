# AGENTS

Spec: `BUILD_SPEC.md` (normative). Handoff: `AGENT_PROMPT.md`. Status: `docs/VALIDATION.md` — update it, don't create parallel planning docs. `REVIEW.md` is historical. Stop at trial artifact; no second weapon, installer, cloud, or roadmap.

## Commands (order matters)

```pwsh
pwsh -ExecutionPolicy Bypass -File scripts/setup-model.ps1   # fresh clone first: tessdata/*.traineddata is gitignored, SHA-pinned
dotnet build Sitrep.slnx -c Release                    # .slnx, not .sln
dotnet test tests/Sitrep.Tests -c Release
pwsh -ExecutionPolicy Bypass -File scripts/verify.ps1        # canonical gate: locked restore + build + test + publish + packaged self-test/diagnose
```

- Single test: `dotnet test tests/Sitrep.Tests -c Release --filter "FullyQualifiedName~GeoMath"`
- Publish: `scripts/publish.ps1` → `out/Sitrep-win-x64/` + `.zip` (gitignored). Test the published exe, not just `dotnet run`, and from an unrelated CWD.
- Diagnostics (live `RecognitionPipeline`, exit 2 on OCR reject): `Sitrep.exe --self-test` (dependency smoke only, not accuracy proof); `Sitrep.exe --diagnose-image <png> --crop x,y,w,h --report out.json` (known-good crops in `docs/VALIDATION.md`). Use `Invoke-PackagedCheck` from `scripts/common.ps1` to wait for this WinExe and enforce its exit code.
- `scripts/verify.ps1` runs public clean-clone checks; add `-PrivateFixtures` to also verify the two gitignored user screenshots. `--integration-test` runs packaged orchestration/Win32 regressions, not real-game validation.

## Structure

- `src/Sitrep.Core` (`net10.0`, portable): parser, `GeoMath`, `FiringTable`, `SolutionState`, `CaptureRegion`, `Data/l81-apollyon.json` + `PROVENANCE.md`. No WPF/Win32 here — tests must run without Windows/game.
- `src/Sitrep.Desktop` (`net10.0-windows`, WPF `WinExe`, `x64`/`win-x64`): control + click-through overlay windows, `InputMonitor`, `ScreenCapture`, `OcrEngine`, `RecognitionPipeline`, `AssistantService`, `AppConfig`.
- `tests/Sitrep.Tests` (xunit, pure deterministic): parser, math, table, state races, ROI. Image replay must reuse the live recognition path.
- SDK pinned in `global.json` (10.0.303, `rollForward: disable`); `packages.lock.json` committed, `RestorePackagesWithLockFile` on, CI/verify use `--locked-mode`. CI is Windows-only (`.github/workflows/ci.yml`).

## Gotchas

- Bundled data: `AppContext.BaseDirectory` (`data/`, `tessdata/`). Writable: `%LocalAppData%/Sitrep/` (`config.json`, `captures/`, 50 files max). Missing model/data must fail startup with one actionable error — never silent download or plausible output.
- Config defaults: `ForegroundTitleContains=""` leaves capture disabled; live starts disabled. Keys: F8 origin, F7/MMB target (same pipeline), F9 clear, F10 enable/disable. Closing control window must exit overlay/workers.
- State: invalidate solution at request start (`READING`); origin failure leaves no usable origin; target failure keeps origin but kills target/solution (`TARGET OCR FAILED`); F9/disable/foreground-loss/origin-replace discard pending work (generation/revision guards — stale OCR completions never commit). One OCR worker, newest request only, off UI thread.
- Parser: strict — invariant culture, 2 fractional digits, full token boundaries; reject partial/conflicting/missing-axis; no letter→digit repair, no invented decimals or bounds.
- Math: 100 m/grid, `range`/`atan2(dx,dy)` bearing in [0,360); fixture origin (101.53,107.77) → target (101.66,110.10) = 233.362379 m, 3.193449° (1e-6 tol), ~755.638 MIL from 229→760/239→750 linear. L81 only, 132–684 m, no extrapolation/terrain; `OUT OF RANGE` / `TABLE UNVERIFIED`, never a guessed MIL.
- Style: nullable + latest analyzers + `EnforceCodeStyleInBuild`; `.editorconfig` LF, trim, final newline (4-space cs, 2-space md/json/ps1). Fix warnings, don't suppress.
- Never: game-memory read/write, injection, hooks, input synthesis, anti-cheat evasion, admin requests, network/telemetry. Local-only; publisher permission unresolved — get explicit approval before in-game use. Keep user screenshots/crops local (`captures/`, `fixtures-local/`, `out/`, `*.zip`, `TestResults/` are gitignored).
