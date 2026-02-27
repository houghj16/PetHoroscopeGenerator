# Routing Rules

## Signal → Agent

| Signal Pattern | Route To | Notes |
|---------------|----------|-------|
| Blazor, UI, component, page, layout, CSS, Razor | Mouth | Frontend work |
| API, endpoint, service, database, backend, model | Data | Backend work |
| Test, quality, bug, edge case, coverage | Stef | Testing work |
| Architecture, design, review, decision, scope | Mikey | Lead decisions |
| Multi-domain, "team", broad feature | Mikey + relevant agents | Fan-out |
| Issue triage, PR review | Mikey | Lead triages |

## File Ownership

| Path Pattern | Primary | Secondary |
|-------------|---------|-----------|
| PetHoroscopeGenerator.Web/** | Mouth | Mikey |
| PetHoroscopeGenerator.ApiService/** | Data | Mikey |
| PetHoroscopeGenerator.AppHost/** | Data | Mikey |
| PetHoroscopeGenerator.ServiceDefaults/** | Data | Mikey |
| PetHoroscopeGenerator.Tests/** | Stef | Data, Mouth |
| *.csproj, *.sln | Mikey | Data |
