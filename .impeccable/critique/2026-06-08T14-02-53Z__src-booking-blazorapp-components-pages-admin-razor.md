---
target: admin dashboard
total_score: 22
p0_count: 0
p1_count: 3
timestamp: 2026-06-08T14-02-53Z
slug: src-booking-blazorapp-components-pages-admin-razor
---
## Design Health Score

| # | Heuristic | Score | Key Issue |
|---|-----------|-------|-----------|
| 1 | Visibility of System Status | 3 | Good loading/error states; no live refresh indicator or last-updated timestamp |
| 2 | Match System / Real World | 3 | Dutch throughout; "Bezorging" nav section hosting "Integraties" is a mild mismatch |
| 3 | User Control and Freedom | 2 | Navigation-only page; no undo, no breadcrumbs system-wide |
| 4 | Consistency and Standards | 2 | Two incompatible icon paradigms: CSS dot-bullets in nav, raw letters (R/O/M/P) in quick actions |
| 5 | Error Prevention | 2 | No destructive actions here; downstream reservation status changes lack confirmation |
| 6 | Recognition Rather Than Recall | 2 | All nav icons render as identical dots; quick action icons are cryptic single letters |
| 7 | Flexibility and Efficiency | 2 | No keyboard shortcuts; all 4 stat cards deep-link to the same unfiltered page |
| 8 | Aesthetic and Minimalist Design | 3 | Calm, structured layout; dark sidebar + warm accent is distinctive and considered |
| 9 | Error Recovery | 2 | API error has no retry; Blazor error UI is English in a Dutch app |
| 10 | Help and Documentation | 1 | Zero contextual help, zero onboarding, zero setup guidance for new owners |
| **Total** | | **22/40** | **Acceptable** |

## Anti-Patterns Verdict
Not AI-generated at the macro level — the dark sidebar, amber mark, and warm palette are distinctive. Two placeholder tells: letter icons (R/O/M/P) in quick actions, and English Blazor error UI. Nav icon classes (nav-icon-home, nav-icon-clock, etc.) render as identical dots — consistent but text-dependent. Deterministic scan: zero findings.

## Priority Issues
P1 Quick action icons are cryptic letters — R/O/M/P require reading label; meaningless as glyphs.
P1 Blazor error UI in English — "An unhandled error has occurred." breaks trust in a Dutch product.
P1 No onboarding — new Owner has no setup prompt; opening hours must be set before booking works.
P2 Staff dashboard nearly empty — 1 quick action; no mijn rooster or bezorgorders shortcut.
P2 All stat cards link to same unfiltered reservations page — no filtered deep-link.

## Persona Red Flags
Alex: clicking stat card goes to unfiltered list; no keyboard shortcuts.
Sam: loading-state has no aria-live; NavLink sets .active CSS but not aria-current.
Fatima (owner): no "new since last check" indicator; no delivery order count on home screen.
