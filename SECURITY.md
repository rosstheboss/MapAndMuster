# Security policy

## Report a vulnerability

Do **not** open a public GitHub issue, pull request, or discussion for a security report.

Use GitHub's private vulnerability reporting on this repository (Security → Advisories →
Report a vulnerability). Include enough detail to reproduce the issue and nothing that
exposes other people's campaign or account data.

Please do not include:

- production credentials or tokens
- player email addresses
- hidden orders, unrevealed relics, or private objectives from a live campaign

## Scope

In scope: authentication and authorization bugs, secrecy leaks in API responses, injection
or upload issues, secret exposure in this repository, and CI/supply-chain issues in this
repo.

Out of scope: reports that depend only on a self-hosted operator's secrets, firewall, or
host misconfiguration; theoretical issues with no practical impact; and social-engineering
the maintainer.

Product secrecy and authorization rules for the application itself are in `docs/SECURITY.md`.

## Response

This is a non-commercial project maintained in spare time. You should get an acknowledgement
when the report is seen. There is no bug bounty.
