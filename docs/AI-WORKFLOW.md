# AI-Assisted Development Workflow

## Canonical context

Agents begin with `AGENTS.md`, then the nearest nested instructions. Product truth lives in
`docs/`, not in provider memory or chat history. Update the documents when a decision changes.

## Task prompt checklist

A good task identifies:

- user-visible outcome;
- affected domain module;
- acceptance criteria;
- permissions and secrecy expectations;
- expected tests;
- explicitly excluded work;
- relevant decision/document links.

## Required agent behavior

Before editing, the agent should summarize applicable invariants and inspect related tests.
After editing, it should report changed files, behavior, migrations/contracts, and verification
commands. It must state any check it could not run.

Agents must stop and request a decision when behavior is listed in `DECISIONS-NEEDED.md`,
when two rules conflict, or when a change would add an unapproved dependency/architecture.

## Keeping providers consistent

- `AGENTS.md` is canonical.
- `CLAUDE.md`, Copilot instructions, and Cursor rules point to it.
- Provider adapters contain only discovery hints, not duplicate domain rules.
- Nested `AGENTS.md` files add local constraints for Domain, API, Web, and tests.

## Review checklist

- Does code belong in the selected project/module?
- Does the server enforce the rule and permission?
- Could any response/log/cache reveal hidden data?
- Are original records and actor attribution preserved?
- Are time, randomness, and concurrent edits deterministic/testable?
- Do tests cover invalid and unauthorized cases as well as success?
- Did generated code introduce proprietary game content?
- Were docs and API clients regenerated where required?
