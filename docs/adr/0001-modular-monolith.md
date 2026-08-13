# ADR 0001: Modular Monolith with Two Deployable Applications

- Status: Accepted
- Date: 2026-08-10

## Context

The application is initially developed by one experienced developer with AI assistance and must
support a roughly twenty-player campaign quickly. Its domain includes private simultaneous
orders, map resolution, audit, corrections, battles, supply graphs, objectives, and relics.

## Decision

Use a modular monolith. The backend is divided into Domain, Application, Infrastructure, and
API projects. Angular is a separate web application. PostgreSQL is shared by backend modules.
Only API and Web are deployed.

## Consequences

- Project references enforce separation of campaign rules from web/database concerns.
- Unit tests can exercise domain rules without infrastructure.
- Deployment and local development remain simple.
- Cross-module transactions and reporting remain straightforward.
- Additional projects add minor solution structure but not operational services.
- A future service extraction requires a separate ADR and demonstrated need.
