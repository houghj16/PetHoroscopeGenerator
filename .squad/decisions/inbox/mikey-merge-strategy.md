# PR Merge Strategy Decision

**Date:** 2026-02-27  
**Decision Maker:** Mikey (Lead)  
**Context:** Managing 5 open PRs with overlapping functionality

## Decision
When multiple PRs implement the same feature, prioritize **quality over chronology**:
1. Evaluate implementations on merit (UX, completeness, maintainability)
2. Merge the superior implementation first
3. Resolve conflicts by keeping the better version
4. Close superseded PRs with clear communication

## Rationale
- PR #17 (Wizzlebop's Mystical Botanicals) had richer UX than PR #13/#16/#19's simpler Plants pages
- PR #19 had unique value (layout components, tests) worth preserving despite conflict
- Users benefit more from the best implementation than from merge order

## Outcomes
- **Merged**: PR #12 (image gen), PR #17 (Plants - best impl), PR #19 (layout components)
- **Superseded**: PR #13 (basic Plants), PR #16 (Plants w/ carousel)
- Resolved conflicts by preferring PR #17's Plants.razor

## Lessons
- Parallel feature development requires active coordination
- Quality beats speed in user-facing features
- Manual PR closure needed when gh CLI unavailable
