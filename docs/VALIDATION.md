# VALIDATION

## Decisions
- Scope: local Windows L81 trial, not a public production release or game-compatibility claim. SDK 10.0.303, WPF win-x64; Core/tests remain portable. No new package dependencies. Official checkout 7.0.1/setup-dotnet 6.0.0 are SHA-pinned (Node 24), replacing the CI-observed deprecated Node 20 action runtimes.
- Tesseract 5.2.0; official English fast model pinned by revision and SHA256 in `scripts/setup-model.ps1` and `src/Sitrep.Desktop/tessdata/README.md`. No runtime downloads.
- Apollyon L81 revision `e8d1ee9ceb07fe9ec4923b7b55e836f8b3bb3e05`: 132–684 m, linear interpolation, no extrapolation/terrain correction. All 84 upstream samples and limits independently compared with the local profile: exact match. See `src/Sitrep.Core/Data/PROVENANCE.md` for rights limitations.
- Live starts disabled. F10 remains observed while disabled; capture keys do not. Foreground polling binds one HWND; loss hides the live overlay and invalidates targets, closure clears origin. Enabling/config changes reseed input; saving Settings clears window attachment/origin.
- Snapshot on the input dispatcher at the frozen event anchor/ROI, then one 50 ms retry before OCR. Reject movement, delayed retry (>500 ms), clipping, any own-window overlap, or disagreeing valid recipes/snapshots. One OCR worker, at most one replaceable pending item; drain before engine disposal.

## Audit resolution (010-Sitrep-audit.md)
| Findings | Root fix / regression evidence |
| --- | --- |
| P0-1 origin replacement | Invalidate usable origin immediately; target cannot supersede pending origin. `StateTests` and packaged orchestration regression. |
| P0-2 frozen capture / queue | Capture before OCR, preserve request ROI, dispose superseded frames. `LatestCaptureWorkerTests` and packaged busy-worker snapshot test. |
| P0-3 DPI / contamination / bounds | Native physical `GetWindowRect`, client/monitor intersection, exclude all visible SITREP windows. Negative-origin/clipping unit tests and native window/style checks at 96 DPI. Mixed-DPI physical trial still pending. |
| P0-4 foreground / closure | Continuous observer plus checks before both captures and publication. `ForegroundSessionTests`, delayed focus/closure completion checks. |
| P0-5 and P2 shutdown / failures | Owned draining worker, caught capture/OCR failures, UI-only state publication. Exception recovery/disposal and late-completion tests. |
| P1-1 through P1-4 | Await real WinExe exit with redirected streams/timeout; five mandatory negative images; public/private gates separated; locked publishing; fail-closed model checksum and immutable action/model pins. Real native/GUI nonzero-exit and missing/corrupt dependency probes. |
| P2 resources / robustness | Borrow input bitmap, dispose intermediates, LockBits grayscale/threshold pixel-parity check; validated/atomic config persistence with one startup error; corrupt table types fail closed and constructor private; F10 reseeding; normalized bearing display; explicit MOVED status; 50-file debug retention; bounded retry and consensus tests. |
| P3 hygiene / docs | Removed obsolete capture/gate helpers, redundant statuses/branches/settings, unused coverage package; x64 checked style APIs; warnings as errors; data copied to test output; RID rationale; correct Sitrep paths/overlay fields; packaged notices/model README and pinned table provenance. Rights inventory caveat below remains open. |

## Executed checks (Windows workstation, 2026-09-16)
- `dotnet --version`: 10.0.303. `dotnet build Sitrep.slnx -c Release`: 0 warnings, 0 errors.
- `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 -PrivateFixtures`: **exit 0 / VERIFY OK**. Locked restore/build/publish, **117 passed / 0 failed / 0 skipped**, packaged self-test/integration, failure probes, five negative images and both private positives. Executables run from unrelated temporary CWD; user config is not touched by diagnostics.
- Startup regression first reproduced six missing-actionable-message failures: `InvalidDataException` is not an `IOException`. Fixed contextual load errors and interactive catch path; full suite and packaged startup-handler checks now pass.
- Self-test accepts exactly x101.53 y107.77 using the live OCR path. Packaged integration checks preprocessing pixel parity/ownership, native overlay bounds/styles, invalid startup config, debug retention, frozen/busy snapshot queue, retry movement, OCR exceptions, and completion focus/closure guards.
- All five committed negative images reject (exit 2); none accepted. `conflict.png` is truncated and rejects on precision, so true recipe/snapshot conflicts also have deterministic regressions.
- Private `MapCords.png --crop 900,640,300,180`: x101.53 y107.77; `MapPing.png --crop 900,300,300,160`: x101.66 y110.10; both exit 0. Kept local, not committed/uploaded.
- Calculation fixture: 233.36237914454 m, 3.19344927328°, 755.63762085546 MIL. Independent table checks: 132→850, 229→760, 239→750, 300→690, 684→150; interior 400→583.33333333333, 500→461.42857142857; exact/outside limits tested. Display rounds MIL to 0.1; bearing rounds then normalizes below 360°.
- Local artifact: `out/Sitrep-win-x64/` and `out/Sitrep-win-x64.zip`, with notices/provenance/model. Visual C++ x64 runtime is a separate prerequisite. No release or installer produced.

## Blockers / pending
- PR #3: initial `f97c58c` Windows push/PR verification and CodeQL passed; SonarCloud reported 14 annotations. Follow-up preserves exact profile matching (with near-limit rejection tests), makes callback observations explicit in integration tests, splits pixel-check complexity, and clarifies nullable/branch/interop/output handling without suppressions. Latest CI and review status live on the PR; earlier success is not evidence for a newer head or reviewer approval.
- **Not game-validated:** publisher approval, real click-through/MMB delivery and trigger loss under load, cursor-label alignment/edges/zoom/UI scale, mixed-DPI displays, rapid targets/F9/F10/alt-tab/exit in game, 30 captures, p95 latency, idle CPU and repeated-OCR memory measurements. Native styles at 96 DPI and fake-boundary tests do not prove these.
- **Redistribution review remains open:** upstream excludes third-party material from its MIT grant; table authorship/rights are not independently established. Native codec/subdependency inventory is not fully supplied by the wrapper package. Top-level Tesseract, Leptonica (BSD, not Apache), InteropDotNet, model, data and SDK runtime notices are included, but this is not complete legal clearance for public binary distribution.
- Completion label: **Implemented, offline trial gates passed**. No blanket production/live-use all-clear; remaining real-input and rights checks must not be inferred from synthetic tests.

## Next step
Confirm publisher permission and redistribution scope, then perform the BUILD_SPEC Gate E checklist on the actual supported display/game configuration. Keep the trial local and live disabled until permission is resolved.
