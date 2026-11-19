---
type: lesson
project: vibe-ui
date: 2026-07-13
tags: [lang/dotnet, area/testing, area/ci]
---
# CI Host Lifecycle And Transition Timing

## Compatibility Hosts

A managed compatibility server must not be reused while its previous process tree is still exiting. A fire-and-forget `Process.Kill` allowed the next xUnit case to observe a non-exited process, reuse it, and then spend 90 seconds probing a dead port on Linux.

Serialize start and stop with a lifecycle gate, wait for `WaitForExitAsync` with a bounded timeout, and release the server in an exception-safe `finally` path. Stress the actual route sequence, not only isolated host startup.

## Animated Component Tests

bUnit's default one-second assertion timeout is too narrow for callbacks intentionally delayed by CSS transition timing when the full suite runs under coverage. Use an explicit bounded transition timeout for delayed callbacks while still asserting the exact result.

The `Drawer` and `Sheet` suites use a five-second test timeout for 300 ms close transitions. This does not lengthen successful tests; it only gives slow CI workers enough headroom.

## Evidence

- Compatibility lifecycle stress: 25/25 local cases passed.
- Full UI coverage command: 1,973/1,973 tests passed.
- Exact-SHA GitHub CI: [29303898506](https://github.com/Narcoleptic-Fox/Vibe.UI/actions/runs/29303898506).
