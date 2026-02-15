# Contributing

## Development Setup
1. Install .NET SDK `10.0.103` (or a compatible feature roll-forward from `global.json`).
2. Restore the solution: `dotnet restore src/RadioPulse.sln`.
3. Build the solution: `dotnet build src/RadioPulse.sln -c Debug`.

## Branching and Commits
1. Create feature branches from `main`.
2. Keep each commit focused and buildable.
3. Use conventional, explicit commit messages.

## Code Quality
1. Run format checks: `dotnet format src/RadioPulse.sln --verify-no-changes`.
2. Keep analyzers clean; warnings are treated as errors.
3. Add or update tests for behavior changes.

## Pull Requests
1. Describe user impact and technical approach.
2. Include verification steps and screenshots for UI changes.
3. Link related issues.
