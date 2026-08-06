# Step 09 — Persisting ink per page (serialize to SQLite) + autosave

## Goal
Save and load ink data for each page using SQLite, with a reliable autosave strategy.

## Deliverables
- Ink serialization format
- Save/load pipeline wired to `PageViewModel`
- Debounced autosave

## Serialization format (MVP)
- Store a single blob per page: `PageInk.InkData`.
- Suggested approach:
  - JSON serialize strokes (points + style)
  - compress (optional, later)

## Save/load lifecycle
- Load ink when a page becomes visible/active.
- Save ink:
  - After stroke completes (debounced, e.g., 500ms)
  - On page/tab switch
  - On app suspend/close

## Repository
- `IInkRepository`
  - `Task<byte[]?> LoadAsync(Guid pageId)`
  - `Task SaveAsync(Guid pageId, byte[] inkData)`

## Error-handling
- If ink load fails:
  - show empty canvas (do not crash)
  - log failure

## Acceptance criteria
- Ink survives app restart.
- Autosave does not produce excessive DB writes (debounced).

## References
- Microsoft.Data.Sqlite: https://learn.microsoft.com/dotnet/standard/data/sqlite/
- App lifecycle: https://learn.microsoft.com/windows/apps/windows-app-sdk/app-lifecycle
