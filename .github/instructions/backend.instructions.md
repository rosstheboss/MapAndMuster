---
applyTo: 'src/**/*.cs,tests/**/*.cs,**/*.csproj,**/*.props,**/*.targets'
---

# Backend Instructions

Read `/AGENTS.md`, `/docs/ARCHITECTURE.md`, `/docs/DOMAIN.md`,
`/docs/CODING-STANDARDS.md`, and `/docs/TESTING-STRATEGY.md`.

- Keep Domain independent of EF Core, ASP.NET Core, Identity, and external services.
- Keep use-case orchestration in Application and adapters in Infrastructure.
- Use permission policies, not scattered role-name comparisons.
- Propagate `CancellationToken` through asynchronous I/O.
- Use UTC instants and an injected clock.
- Preserve original orders/results and append audit records for corrections.
- Add domain unit tests or API/database integration tests as appropriate.
