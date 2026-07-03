# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

PrivacyScanner is a .NET 10 WPF (Windows-only) desktop app that recursively scans a directory for
sensitive/personal data, using configurable regex rules and optionally a SpaCy (Python NLP) subprocess
for named-entity recognition (persons, locations, organizations — German model only). Built by an
apprentice at IT-Titans GmbH with AI assistance; treat it as a learning/training project, not an
audited compliance product.

## Commands

```bash
dotnet restore
dotnet build --configuration Release                      # add -p:TreatWarningsAsErrors=true to match CI exactly
dotnet format --verify-no-changes --verbosity diagnostic   # style gate, must be clean before pushing

dotnet test PrivacyScanner.Tests/PrivacyScanner.Tests.csproj
dotnet test PrivacyScanner.Infrastructure.Tests/PrivacyScanner.Infrastructure.Tests.csproj

# single test
dotnet test PrivacyScanner.Infrastructure.Tests/PrivacyScanner.Infrastructure.Tests.csproj --filter "FullyQualifiedName~IbanRegexRuleTests"

dotnet run --project PrivacyScanner.UI
dotnet publish PrivacyScanner.UI/PrivacyScanner.UI.csproj /p:PublishProfile=OneExe   # self-contained single-file win-x64 exe
```

Both test projects run in CI (`.github/workflows/pipeline.yml`, `windows-latest`). The runner must stay
Windows-only because that job also builds/publishes the WPF `.exe` — not because of the tests, which
are themselves plain .NET and platform-independent (verified to also pass under Linux).

`Directory.Build.props` sets `TargetFramework net10.0` (the UI project overrides to `net10.0-windows`),
`Nullable`/`ImplicitUsings` enable for the whole solution. Package versions are centrally pinned via
`Directory.Packages.props` (Central Package Management) — add new package versions there, not in
individual `.csproj` files.

## Architecture

### Project layout (dependency direction top → bottom)

- `PrivacyScanner.UI` — WPF/MVVM. Depends only on `Infrastructure` + `Infrastructure.Contracts`.
- `PrivacyScanner.Infrastructure` — command/query handlers, the scan engine, the SpaCy process bridge.
- `PrivacyScanner.Infrastructure.Contracts` — Mediator command/query/result types and service interfaces
  (`IDirectoryProvider`, `IProcessService`, `IScannerStateService`). Named `Contracts`, not `Interfaces`,
  because it holds DTO-like command/query/result records too, not just interfaces.
- `PrivacyScanner.Model` — plain DTOs/enums shared across all layers.
- `PrivacyScanner.Tests` (runs in CI) / `PrivacyScanner.Infrastructure.Tests` (also runs in CI) — xUnit +
  Moq. Only `PrivacyScanner.Infrastructure`/`.Contracts`/`.Model` are portable (no WPF); UI code is not
  unit tested for exactly that reason (`net10.0-windows` vs. the test projects' plain `net10.0`).

### Mediator / CQRS

All cross-layer calls go through `Mediator` (`Mediator.Abstractions`/`Mediator.SourceGenerator`, DI
registered as singleton in `App.xaml.cs`) — handlers implement `IRequestHandler<TCommand>` or
`IRequestHandler<TCommand, TResult>`, ViewModels and other handlers call `IMediator.Send`/`Publish`,
never a handler directly. `MainViewModel` also implements `INotificationHandler<FoundWarningEvent>` and
`INotificationHandler<FileProcessedEvent>` to receive live scan progress/results.

### Result-object over exceptions

Command handlers that can fail in an expected way (invalid regex, rule not found, ...) return a typed
`...CommandResult` record with `IsSuccess`/`ErrorMessage` and `Success()`/`Failure(string)` static
factories (see `AddRegexRuleCommandResult`, `EditRegexRuleCommandResult`, `DeleteRegexRuleCommandResult`,
`CheckPythonAndSpacyEnvironmentQueryResult`) instead of throwing. Follow this pattern for new
command handlers rather than introducing exception-based control flow.

### Scan pipeline

`StartScannerCommand` → `ProcessScanCommandHandler` enumerates files via `GetAllFilePathsInDirectoryQuery`
(applies directory-name/file-extension blacklists), then per file calls `ScanFileCommand`, which fans out
to `RegexScanCommandHandler` (line-by-line `Regex.Matches` per enabled rule) and/or
`SpaCyScanCommandHandler` (shells out to a bundled `spacy_scan.py` via `IProcessService`) and merges
warnings. Progress (`FileProcessedEvent`) and hits (`FoundWarningEvent`) are published as they're found,
not batched at the end — the UI's log tree and progress bar update live.

A single file that can't be read (locked, deleted mid-scan) or a single inaccessible subdirectory must
never abort the whole scan — this was a real bug class found and fixed in this codebase
(`DirectoryProvider.GetFiles` does a resilient recursive walk that skips and logs bad subdirectories
instead of using `Directory.GetFiles(..., AllDirectories)` directly, which throws on the first
access-denied folder anywhere in the tree; `ProcessScanCommandHandler` catches per-file IO errors and
skips just that file). Keep this resilience in mind when touching the scan loop.

### Regex rules: default + user-defined merge

`DefaultRegexRulesProvider.GetDefaultRules()` ships 8 built-in rules with fixed GUIDs
(`00000001-0000-0000-0000-0000000000XX`). `GetAllRegexRulesQueryHandler` merges these with user-added/
edited rules from `%ProgramData%\PrivacyScanner\rules.json`, **by `RuleId`** — a saved user rule
overrides/replaces a default with the same ID. **Never change a shipped default rule's `RuleId`**; doing
so silently disconnects existing users' enable/disable state for that rule. When adding a new default
rule, use the next unused ID in the sequence (see README's "Extending PrivacyScanner" section for the
full walkthrough). All app state (`rules.json`, directory/file-extension blacklists) lives under
`%ProgramData%\PrivacyScanner\` and is loaded with a corrupted-file fallback (log + revert to
defaults/empty), not a hard failure.

### Naming conventions enforced in this codebase

- Methods returning `Task`/`ValueTask` are suffixed `Async` (Microsoft TAP convention), except where an
  interface/base signature forces otherwise or for `async void` event handlers.
- Constructors validate injected dependencies with `ArgumentNullException.ThrowIfNull(x)`.
- `.editorconfig` governs `var` usage, brace style, and naming — trust it over ad-hoc style choices;
  `dotnet format --verify-no-changes` is a hard CI gate.

### Global exception safety net

`App.xaml.cs` registers `DispatcherUnhandledException` (logs + keeps the UI thread alive),
`AppDomain.UnhandledException`, and `TaskScheduler.UnobservedTaskException` handlers. This exists as a
last-resort net for the many `async void` command handlers in the ViewModels (required by `ICommand`);
individual ViewModel methods are intentionally not all wrapped in their own try/catch — rely on the
global handler rather than adding redundant per-method exception handling there.
