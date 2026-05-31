---
title: EzLib Gemini Agent Memory
type: agent-entrypoint
status: active
project: Ezsoft4u-EzLib.dotnet8.Tools
updated: 2026-05-31
tags:
  - ai-team
  - gemini
---

# Gemini Shared AI Memory

This GitHub project uses centralized AI memory.

Gemini should follow the same shared memory protocol as other agents.

## Central AI Memory

Memory repo:

```text
https://gitlab.ezsoft4u.com/mark/ai-memory
```

Project ID:

```text
Ezsoft4u-EzLib.dotnet8.Tools
```

Project memory path:

```text
PROJECTS/Ezsoft4u-EzLib.dotnet8.Tools/.ai/
```

## Start Of Work

Before making changes:

1. Read this file.
2. Read `.ai/AI_AGENT_GUIDELINES.md`, if present.
3. Read `.ai/AGENTS.md` in this repo, if present.
4. Read the central memory repo `AGENT_PROTOCOL.md`, if accessible.
5. Read `PROJECTS/Ezsoft4u-EzLib.dotnet8.Tools/.ai/PROJECT_STATE.md`, if accessible.
6. Read `PROJECTS/Ezsoft4u-EzLib.dotnet8.Tools/.ai/HANDOFF.md`, if accessible.
7. If central memory is not accessible, read repo-local `.ai/` if present.
8. If neither is available, ask the user for project memory.

If root-level `AGENTS.md` or `GEMINI.md` exists, read it as repo/team guidance. Do not modify root-level agent files unless the user explicitly asks.

Authentication, if needed, must be provided by the execution environment:

```text
GITLAB_AI_MEMORY_URL=https://gitlab.ezsoft4u.com/mark/ai-memory.git
GITLAB_AI_MEMORY_TOKEN=<read-only-token>
GITLAB_AI_MEMORY_WRITE_TOKEN=<write-token-for-trusted-agents-only>
```

Do not write tokens into this repo.

## End Of Work

Before finishing:

1. Update central GitLab memory if you have write access.
2. If you cannot write central memory, output an `AI Memory Patch` in your final response.
3. Include changed files, verification performed, remaining work, and next action.

## Safety

Never write secrets, tokens, passwords, connection strings, personal data, or customer-sensitive data into AI memory.
