# Coding Standards

Automated configuration is authoritative: `.editorconfig`, `Directory.Build.props`, ESLint,
Prettier, and Stylelint. This document explains intent.

## C# and .NET

- Four spaces and Allman braces.
- File-scoped namespaces; one primary public type per file.
- Nullable reference types and implicit usings enabled.
- Use `var` when the assigned type is evident; otherwise prefer an explicit type.
- Prefer clear immutable value objects for identifiers, points, revisions, and time ranges.
- Use async APIs for I/O, suffix methods with `Async`, and propagate `CancellationToken`.
- Use injected time; do not call local time in domain/application code.
- Use exceptions for exceptional failures, not expected invalid player commands.
- Return structured validation/domain errors with stable codes.
- Avoid primitive obsession where it obscures domain invariants.
- Do not expose EF entities directly through API contracts.
- Keep methods cohesive. Extract named policy/domain services for complex resolution rules.
- Primary constructors are allowed only when readability improves; they are not the default.

## Angular and TypeScript

- Two spaces, single quotes, semicolons, trailing commas, and 120-character print width.
- Strict TypeScript and Angular templates; no unexplained `any` or non-null assertions.
- Standalone, zoneless, feature-oriented components.
- Signals for local/synchronous state; RxJS for asynchronous streams and cancellation.
- Prefer typed reactive forms for nontrivial input.
- Components translate user interaction into application intent; they do not resolve campaign
  rules or infer permissions.
- Use semantic HTML before custom widgets. Ensure keyboard, focus, label, contrast, and error
  announcement behavior.
- Keep CSS class names kebab-case and styles component-scoped unless truly global.
- Never edit generated OpenAPI client files.

## Naming

- Use ubiquitous language from `DOMAIN.md`.
- Distinguish Campaign Points (`CampaignPoint`) from Battle Points and Supply Points.
- Use `actualActor` and `effectiveActor` consistently.
- Name UTC values with `Utc` or use an instant type that makes the semantics explicit.
- Avoid unexplained abbreviations such as `CO`, `SP`, or `CP` in public code APIs.

## Comments and documentation

- Comments explain precedence, security, or why an unusual rule exists.
- Public API contracts and difficult domain policies require concise documentation.
- Update architecture/domain documents with behavioral changes.
- Record consequential technical decisions under `docs/adr/`.
