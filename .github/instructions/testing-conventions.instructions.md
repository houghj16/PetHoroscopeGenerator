---
description: "Use when creating or editing tests to enforce xUnit and bUnit conventions for this repo."
applyTo: "PetHoroscopeGenerator.Tests/**/*.cs,**/*Tests.cs"
---

# Testing Conventions

- Use xUnit patterns and clear Arrange, Act, Assert flow.
- Keep each test focused on one behavior and use descriptive test names.
- Prefer deterministic tests by avoiding real network calls and time-sensitive logic.
- For Blazor component tests, use bUnit patterns with explicit render and assertion steps.
- Add or update tests when behavior changes, especially for bug fixes and UI logic updates.
