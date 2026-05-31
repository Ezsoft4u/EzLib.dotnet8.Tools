---
title: EzLib AI Memory Bootstrap
type: bootstrap
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-05-31
tags:
  - ai-team
  - bootstrap
---

# AI Memory Bootstrap

This file is the repo-local AI memory entrypoint for this repo.

Some AI agents do not automatically read `AGENTS.md`, `CLAUDE.md`, `GEMINI.md`, or tool-specific auto memory. Before reading this repo-local file, local trusted agents should read the canonical Obsidian bootstrap:

```text
C:\Users\MARK\Obsidian\AI_Team\BOOTSTRAP.md
```

When starting work in this repo, read this file after the Obsidian bootstrap and follow the loading order below.

## Project Identity

Project ID:

```text
Ezsoft4u-EzLib.dotnet8.Tools
```

Central AI memory repo:

```text
https://gitlab.ezsoft4u.com/mark/ai-memory
```

Central project memory path:

```text
PROJECTS/Ezsoft4u-EzLib.dotnet8.Tools/.ai/
```

## Load Order

After reading the Obsidian bootstrap, read:

1. `.ai/PROJECT_STATE.md`
2. `.ai/HANDOFF.md`
3. `.ai/AI_AGENT_GUIDELINES.md`
4. `.ai/AGENTS.md`
5. `.ai/CLAUDE.md`, if you are Claude
6. `.ai/GEMINI.md`, if you are Gemini
7. Central memory path, if accessible:
   `PROJECTS/Ezsoft4u-EzLib.dotnet8.Tools/.ai/`

If root-level `AGENTS.md`, `CLAUDE.md`, or `GEMINI.md` exist, read them too as repo/team instructions. Do not modify them unless the user explicitly asks.

## Authentication

Authentication must come from the execution environment, never from committed files.

Expected environment variables:

```text
GITLAB_AI_MEMORY_URL=https://gitlab.ezsoft4u.com/mark/ai-memory.git
GITLAB_AI_MEMORY_TOKEN=<read-only-token>
GITLAB_AI_MEMORY_WRITE_TOKEN=<write-token-for-trusted-agents-only>
```

Do not write token values into this repo.

## If Central Memory Is Unavailable

If GitLab central memory cannot be read:

1. Continue using local `.ai/` files.
2. Ask the user for missing project memory if needed.
3. At the end, output an `AI Memory Patch`.

## End Of Work

Before finishing:

1. Update `.ai/HANDOFF.md` if this repo-local memory is writable.
2. Update central memory if you have write access.
3. If you cannot write central memory, output an `AI Memory Patch`.
4. Report changed files, verification performed, remaining work, and next action.

## Safety

Never write secrets, tokens, passwords, connection strings, personal data, or customer-sensitive data into AI memory.

If sensitive configuration exists in this repo, record only that it exists and where agents should avoid copying from. Do not copy the values.
