Implement the WARDOGS Mortar Assistant using the attached/repository-root BUILD_SPEC.md as the product specification, while respecting higher-priority instructions and existing repository guidance.

Read the spec, inspect the current repository and available environment, and begin the smallest implementation that satisfies its gates. Do not rewrite the plan, create alternative architectures, or build cosmetic features.

Before coding, report briefly:
1. Existing repository state and instructions.
2. Whether Windows execution, the required SDK, actual screenshot fixtures, and source-backed firing data are available.
3. Which checks can run here and which require the user's machine.

Prioritize the uncertain integration points: actual capture/label alignment, native OCR packaging, stale-result prevention, and independently checked L81 data. Keep pure logic testable without Windows and image replay on the same recognition path as live capture.

Preserve existing work. Keep one short docs/VALIDATION.md with executed checks, evidence, decisions, and blockers. Distinguish synthetic tests, supplied-image tests, packaged Windows execution, and real-game validation. Never invent test results or approval.

Do not spend repeatedly on a blocker without new evidence. After two distinct unsuccessful fixes, state the exact missing input or failing observation and continue only independent useful work. Use one implementation owner; any delegated review should be narrowly scoped and must not produce a competing implementation.

Deliver reproducible build/test/publish commands, a Windows trial ZIP when actually built, source/data notices, and concise run/diagnostic instructions. Live mode must start disabled, normal game input must pass through, and unavailable/unverified data must never produce a plausible MIL setting.

Stop at “Ready for user trial” when its evidence requirements are met. If they cannot be met here, report “Implemented” with the remaining checks and blockers. Do not claim game validation without a real run, and do not add a feature roadmap. Wait for the user's test results before further iteration.
