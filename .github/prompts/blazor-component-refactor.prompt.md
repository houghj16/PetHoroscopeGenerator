---
mode: ask
model: GPT-5
description: "Refactor a Blazor component by moving complex logic to code-behind/services while preserving behavior and tests."
---

Refactor this Blazor component to improve readability and maintainability.

Goals:
- Keep behavior and visual output unchanged.
- Move complex logic from markup into code-behind or service classes.
- Keep smaller, local logic inline when it improves clarity.
- Use async/await for non-blocking UI operations where applicable.
- Add or update tests when behavior or state flow is affected.

Inputs:
- Target file or component name.
- Any known bugs or constraints.

Output:
- A concise summary of what changed and why.
- The updated files.
- Any test additions or updates.
