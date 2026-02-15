# Technical Decisions

## 2026-02-15

1. The repository was initialized from an empty git root, so all phases are scaffolded from scratch.
2. `.NET SDK 10.0.103` is pinned in `global.json` to keep builds reproducible.
3. `LangVersion` is set to `preview` to align with C# 14.
4. Warnings are treated as errors to enforce quality from the first phase.
5. PHASE 0 includes governance and hygiene artifacts up front:
   - `LICENSE` (MIT)
   - `.github/CODEOWNERS`
   - `CONTRIBUTING.md`
   - `SECURITY.md`
   - GitHub issue templates
