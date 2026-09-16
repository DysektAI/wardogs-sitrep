# Review of the original WARDOGS build plan

Date: 16 September 2026

## Scope and evidence limits

This review used four distinct passes: research, planning, critical review, and final judgment. No independent subagent execution facility was available; these are not separately generated agent reports.

The review inspected primary Windows/.NET/OCR documentation, the linked calculator, a public community calculator's source data and license scope, and the published game terms. It checked the numerical coordinate example independently. The supplied screenshots were interpreted visually, not processed by an OCR engine. No C# application was implemented or executed; no live-game, anti-cheat, performance, or OCR-accuracy claim has been verified.

The revised product specification is BUILD_SPEC.md; its source index provides the research links. AGENT_PROMPT.md is the short implementation handoff. This historical review is not additional product scope.

## Research pass

**Reference sources disagree at the boundary.** Schaulers describes an approximate 120–700 m L81 range. The inspected community data configures 132–684 m and contains additional samples outside those limits. A table's first/last rows are therefore not necessarily valid weapon limits. The build must select and version a reference rather than demand identical results from inconsistent sources. See BUILD_SPEC sources S1–S3.

**OCR choice has deployment consequences.** Microsoft documents package identity for supported Windows.Media.Ocr desktop use. The inspected Tesseract wrapper has native runtime and model dependencies. Neither option justifies assuming a NuGet package plus an executable is a complete deliverable. Prove loading from a published folder early. See S8–S10.

**Input polling is observational, but not lossless.** GetAsyncKeyState's high bit exposes current state; its historical low bit is unreliable. A complete click between polls may be missed. Start simple, measure, and keep Raw Input as an evidence-triggered alternative rather than adding hooks or two parallel monitors. See S12–S13.

**Cursor location and game label location are separate facts.** GetCursorPos returns OS cursor coordinates. The screenshots alone do not prove the game anchors coordinate text to that exact point at all map zooms/UI scales/edges. DPI awareness also does not by itself establish label scaling. See S15–S16 and the provided images.

**Table matching is not in-game accuracy or permission.** Firing tables can change and contain no automatic terrain correction in the selected MVP. The current game terms do not supply an explicit approval for this exact utility. Record permission as unresolved and keep live mode disabled by default. See S1–S4, S18–S19.

## Planning pass

Retain the central workflow, external-pixels-only constraint, one weapon, on-demand recognition, plain overlay, and focused tests. Replace the seven strictly serial stages with a dependency-aware set of gates.

Prove dependency loading and capture localization early. A tiny overlay focus/pass-through smoke test should also happen early: discovering at the end that the presentation approach interferes with the game wastes otherwise avoidable integration work. This is functional testing, not visual polish.

Keep pure parsing/math/state tests runnable without the game. Add image replay through the same OCR pipeline and a published-program dependency self-test. These separate recognizer faults from capture faults and missing native dependencies without requiring every debugging cycle to happen in a match.

Do not make a non-Windows agent wait forever at an in-game gate. Let it complete independent work, record unexecuted checks, and hand off an accurately labelled implementation. Only a successful Windows artifact/runtime check earns “Ready for user trial”; only an actual game run earns “Game-validated.”

Limit investigation branches rather than only discouraging polish. Use one engine, one backend, bounded retries/queues, two initial preprocessing recipes, and specific evidence before changing approaches. Stop adding features when the first trial artifact is ready.

## Critical-review pass: highest-impact failure cases

| Failure case | Required revision |
|---|---|
| New target OCR fails while the old firing solution remains visible | Invalidate the actionable solution at request start; show failure explicitly. Preserve only the confirmed origin. |
| F9/F10 or a new origin is followed by completion of an old OCR task | Request generations and origin revisions; obsolete completions cannot commit. |
| Several pings create concurrent recognition or a growing backlog | One OCR worker, bounded newest-request queue, deterministic disposal. |
| Delayed capture follows a moved cursor rather than the clicked point | Capture/freeze event pixels and anchor; movement/foreground checks on retry. |
| `x10166` is accepted as `x101.66`, or a regex matches a truncated prefix | Strict decimal/token boundaries; do not manufacture digits or decimal points. |
| X and Y come from different frames or conflicting candidates | Treat one frame's pair as an atomic result; reject conflicting pairs. |
| Whitelist/normalization turns background text into plausible numbers | Treat whitelist/confidence as hints; preserve raw tokens and add negative fixtures. |
| Overlay's displayed coordinates are captured as new game coordinates | Prevent or reject ROI overlap with application windows. |
| User cannot close a click-through, non-activating overlay | Provide a normal control window with an application-wide Exit action. |
| Normal MMB timing is unreliable | F7 target capture uses the same pipeline without ping timing. |
| App works on developer machine but not in published output | Pin SDK/packages/model and test native loading from the actual publish directory. |
| Reference data has samples outside usable weapon limits | Validate table coverage and weapon constraints independently. |
| Agent claims full functionality from passing numerical tests | Report implementation, Windows runtime, supplied-image, and game validation separately. |

## Coverage of the original 30 sections

| Original sections | Disposition |
|---|---|
| 1, 23, 26, 30: priorities, phases, non-goals | Retained intent; consolidated and bounded exploration/stop rules. |
| 2: game interaction constraints | Retained; clarified foreground metadata versus memory access and unresolved permission. |
| 3: user workflow | Retained; added one target hotkey fallback and usable exit path. |
| 4: screenshot behavior | Retained fixture text; removed assumed OCR success, resolution, and universal cursor alignment. |
| 5: stack | Stable baseline, exact SDK/package policy, and native-loading proof. |
| 6: DPI | Expanded to explicit coordinate spaces, monitor origins, UI scale, and clipped captures. |
| 7: middle mouse | Kept simple observer; added transition handling and missed-event verification. |
| 8–9: ROI and timing | Event-bound captures, bounded retry, cursor-movement checks, and one measured fallback. |
| 10–13: OCR/preprocessing/parser/validation | Strict acceptance, limited normalization, ambiguous-pair rejection, and no invented bounds. |
| 14: diagnostics | Local, opt-in, bounded ROI records; no gameplay uploads or full-screen recording. |
| 15–16: math and fixture | Confirmed values; added zero-distance, rounding, and non-finite cases. |
| 17: ballistics | Source/version provenance, inconsistent-reference handling, mechanical limits, and independent golden values. |
| 18: overlay | Focus/pass-through smoke test, self-capture prevention, and explicit lifecycle. |
| 19: organization | Small portable core plus Windows adapter and tests; no framework expansion. |
| 20: tests | Added race/state/negative cases, shared image replay, and packaged smoke test. |
| 21–22: repository and commits | Added reproducibility and a small Windows CI job; preserve user work and repository permissions. |
| 24: failure handling | Replaced ambiguous preservation rules with explicit actionable-state invalidation. |
| 25: performance | Measure latency/idle CPU and bounded work; no “negligible” claim without evidence. |
| 27–29: done/validation/handoff | Separated implementation from trial readiness and real-game validation. |

## Final judgment

The original concept is worth keeping, but the original prompt should not be used unchanged. Its strongest elements are its narrow scope and insistence on proving OCR. Its weakest elements are acceptance ambiguity, deployment assumptions, and treating the live loop as a sequence without specifying the asynchronous state rules.

The revised specification addresses those problems without introducing a second weapon, map reconstruction, an installer, a cloud service, or a UI-polish phase. The additions—request validity, a target hotkey fallback, image replay, dependency smoke tests, provenance, and minimal CI—serve the first working trial rather than a future product roadmap.

The remaining uncertainty is empirical: the user's game/display combination must show that the cursor-relative image contains the correct labels and that those labels are recognized reliably. The selected ballistic profile also needs current-game comparison. Neither uncertainty can be removed by more planning alone.

Recommended next action: give the implementation agent BUILD_SPEC.md and AGENT_PROMPT.md, with the original-resolution screenshots available locally under distinct filenames. Do not ask it to repeat this full review before starting.
