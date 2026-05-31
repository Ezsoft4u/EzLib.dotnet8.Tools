---
title: EzLib AI Agent Guidelines
type: guidelines
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-05-31
tags:
  - ai-team
  - guidelines
---

# AI Agent Guidelines

This file is the shared behavior contract for Codex, Claude, Gemini, and other coding agents working in this repo.

Tool-specific files such as `.ai/AGENTS.md`, `.ai/CLAUDE.md`, and `.ai/GEMINI.md` may add runtime-specific details, but they should not fork these core rules.

## Coding Agent Guardrails

1. State assumptions before coding. If intent, scope, or tradeoff is unclear, ask instead of guessing.
2. Prefer the smallest working change. Do not add speculative features, abstractions, configurability, or broad error handling unless required.
3. Make surgical edits. Touch only files and lines needed for the request; do not drive-by refactor, restyle, rename, or delete unrelated code.
4. Read before writing. Inspect nearby exports, callers, tests, utilities, and existing patterns before changing behavior.
5. Existing convention beats novelty. Follow the codebase pattern even if another style seems cleaner; surface disagreement separately.
6. Surface conflicts explicitly. If two patterns or requirements disagree, name the conflict, choose one with rationale, and do not average both.
7. Use deterministic code for deterministic decisions. Do not route retry policy, status-code handling, parsing, or fixed transformations through an LLM.
8. Tests must protect intent, not just execute code. Prefer tests that fail when business behavior is wrong.
9. Verify before claiming done. Report exactly what was run; if tests, migration, or checks were skipped or partial, say so plainly.
10. Fail visibly. Do not hide skipped records, ignored errors, partial results, uncertain assumptions, or unverifiable claims.
11. Checkpoint long tasks. After major steps, summarize what changed, what was verified, and what remains.
12. Keep agent rules short. Prefer concrete imperatives tied to observed failure modes; avoid vague prompts like "be careful" or identity prompts like "act senior".
13. When AI adds or modifies code, unless the user explicitly asks otherwise, add minimal Chinese comments to every added or changed method. Keep comments concise and focused on method purpose or non-obvious intent; avoid noisy line-by-line narration.

## Relationship To Agent-Specific Files

- Codex and general agents read `.ai/AGENTS.md`.
- Claude reads `.ai/CLAUDE.md` and should also read `.ai/AGENTS.md` when present.
- Gemini reads `.ai/GEMINI.md` and should also read `.ai/AGENTS.md` when present.
- All agents should load this guideline before tool-specific or project-specific preferences.

If a project has stricter local rules, follow the project rules unless they conflict with safety, secrets handling, or the shared memory protocol.
