---
type: reference
project: vibe-ui
status: beta-candidate
date: 2026-07-13
commit: 215939d442d075c672033811149cb0d530bc7e13
pr: 6
tags: [lang/dotnet, area/ui]
---
# Vibe.UI Beta Readiness

## Decision

Commit `215939d442d075c672033811149cb0d530bc7e13` is a beta-ready code candidate. PR [#6](https://github.com/Narcoleptic-Fox/Vibe.UI/pull/6) is mergeable, has a clean merge state, and is ready for review.

This does not mean the packages have been published. Production promotion remains an explicit manual action after merge or creation of a matching beta tag.

## Validated Surface

- .NET 10 is pinned with `global.json`; shipping projects build with zero warnings.
- Unit suites pass: 1,973 UI, 366 CSS, and 204 CLI tests, 2,543 total.
- Browser suites pass for docs integration, smoke, functional interaction, and reviewed visual baselines.
- Compatibility tests cover standalone Blazor WebAssembly plus Blazor Web App static SSR, Interactive Server, Interactive WebAssembly, and Interactive Auto.
- Compatibility runs cover Chromium, Firefox, and WebKit and exercise package-built fixtures.
- The CSS beta Core Profile uses strict scanning and fails on unknown utilities.
- Isolated package validation covers `Vibe.UI`, `Vibe.UI.CSS`, and `Vibe.UI.CLI`, including generated standalone and hosted consumers.
- Exact-SHA PR CI run [29303898506](https://github.com/Narcoleptic-Fox/Vibe.UI/actions/runs/29303898506) passed test, Windows package validation, visual regression, and Codecov.
- Exact-SHA NuGet dry run [29303913377](https://github.com/Narcoleptic-Fox/Vibe.UI/actions/runs/29303913377) passed validation and packaging, uploaded the `nuget-packages` artifact, and skipped production promotion.

## Release Controls

- The `nuget-production` GitHub environment requires review by `Dieshen` or `vadManuel` and prevents self-approval.
- Deployment policy permits only `main` and matching `1.0.0-beta*` or `v1.0.0-beta*` tags.
- The existing `NUGET_API_KEY` repository secret is available to the environment-scoped promotion job.
- The workflow accepts only `1.0.0-beta` or numbered beta suffixes, validates project versions, promotes only previously validated artifacts, and requires manual dispatch.
- No NuGet package was published during beta hardening.

## Product Boundary

The beta contract is a documented, deterministic Tailwind-like Core Profile plus shadcn-quality component behavior for supported surfaces. Full Tailwind utility parity and broad shadcn ecosystem parity remain v1 goals, not beta blockers.

Remaining release follow-up includes formal manual screen-reader, 200% zoom, and forced-colors review; broader visual baselines; trusted publishing to replace the long-lived API key; merge approval; and the explicit first beta promotion.
