# Contributing

Map & Muster is MIT-licensed. The maintainer reviews and merges pull requests. Opening an
issue or pull request does not mean it will be accepted.

By contributing, you license your contribution under the MIT License in `LICENSE`.

## Before you start

1. Read `README.md` and `docs/SETUP.md`.
2. For campaign behavior, follow `docs/DOMAIN.md`, `docs/PRODUCT.md`, and
   `docs/CAMPAIGN-RULES-MATRIX.md`. Do not infer rules from tabletop lore.
3. Unresolved product questions belong in `docs/DECISIONS-NEEDED.md`. Do not invent answers.
4. Coding assistants must follow `AGENTS.md`.

Large or architectural changes should start as an issue so review time is not wasted.

## Pull requests

- Keep the change small and cohesive.
- Add or update tests with the behavior change.
- Update docs when behavior, contracts, setup, or architecture change.
- Do not commit secrets, production data, player emails, or unrevealed campaign facts.
- Keep bundled maps, factions, copy, and artwork generic and fictional. Do not add
  proprietary rules text, logos, or game artwork.
- Do not add a component library, job scheduler, cloud provider, or extra identity
  provider without an existing architecture decision.

CI must pass. The maintainer may still request changes or close the pull request.

## Local verification

From the repository root, run `eng/verify.ps1` or `eng/verify.sh`, or the commands listed in
`AGENTS.md`. Run the narrowest relevant checks while iterating.

## Issues

Use the Bug or Feature templates. Do not file public issues for security reports; see
`SECURITY.md`.
