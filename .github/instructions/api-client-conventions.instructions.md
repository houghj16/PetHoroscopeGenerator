---
description: "Use when editing API client classes to keep HTTP access consistent and resilient."
applyTo: "**/*ApiClient.cs"
---

# API Client Conventions

- Keep API clients focused on HTTP communication and mapping of request and response models.
- Use async methods with CancellationToken support where practical.
- Validate and handle non-success responses with clear error messages or typed failures.
- Keep endpoint paths and payload handling explicit and easy to review.
- Avoid leaking transport-level concerns into UI components; keep UI-facing return shapes simple.
