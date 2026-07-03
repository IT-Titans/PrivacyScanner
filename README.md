# PrivacyScanner
 
PrivacyScanner is a .NET tool for scanning files for sensitive and personal data.
Detection is done through **configurable regex patterns** and optionally via **SpaCy** (Natural Language Processing).
 
> **Note:** PrivacyScanner was built by an apprentice at IT-Titans GmbH as part of
> their professional training, with the assistance of AI coding tools during design and implementation. It should be
> read as a learning and training project rather than an audited, production-hardened compliance product. In
> particular, detection results (regex and SpaCy) are a starting point for manual review, not a guarantee of
> complete or legally certified GDPR compliance — see [Known Limitations](#known-limitations).
 
---
 
## Overview
 
PrivacyScanner searches files for defined patterns (regexes) to identify potentially sensitive content.
Additionally, SpaCy can optionally be used to automatically detect personal data such as names, locations, or organizations.
 
---
 
## Features
 
* Recursive directory scan with live progress, file counter, and cancel support
* Regex-based detection: 8 built-in GDPR-oriented rules (see below) plus user-defined custom rules,
  each individually enable-/disable-, edit- and deletable via the UI
* Optional SpaCy (NLP) integration to additionally detect persons, locations, and organizations in
  free text (German language model only)
* Directory-name and file-extension blacklists to exclude folders (e.g. `.git`, `node_modules`) and
  file types from a scan
* Automatic detection and skipping of binary files
* Results grouped by file → detection type (Regex/SpaCy) → rule/label, with a 3-line context preview
  (previous/hit/next line) per match and click-to-open-file
* CSV export of all findings
* Settings (regex rules, blacklists) are stored per machine under `%ProgramData%\PrivacyScanner` and
  persist across app updates
 
---
 
## How Scanning Works
 
1. Pick a root directory to scan (optionally a CSV file to export findings to).
2. The tool enumerates all files under that directory, skipping blacklisted directories/extensions and
   binary files.
3. Each remaining file is scanned line-by-line:
   * every **enabled** regex rule is applied via .NET `Regex.Matches`, and/or
   * if "Use SpaCy" is checked, the file's text is additionally sent to a Python/SpaCy subprocess for
     named-entity recognition (persons, locations, organizations).
4. Matches are shown live in a grouped, expandable results tree with a short context snippet, and can be
   exported to CSV at any point.
 
---
 
## Getting Started
 
### Run the prebuilt executable
 
Every successful build on `main` publishes a self-contained, single-file `Privacy Scanner.exe` (win-x64,
no separate .NET install required) as a build artifact via the [CI/CD pipeline](.github/workflows/pipeline.yml).
Download it, run it — regex-based scanning works immediately. If you also want SpaCy-based detection,
follow [install.md](install.md) first.
 
### Build and run from source
 
Requirements: **.NET 10 SDK** and **Windows 10/11** (the UI project is WPF, which is Windows-only).
 
```bash
dotnet restore
dotnet build --configuration Release
dotnet run --project PrivacyScanner.UI
```
 
Alternatively, open `PrivacyScanner.sln` in Visual Studio and run/debug `PrivacyScanner.UI` directly.
 
### Running the tests
 
```bash
dotnet test PrivacyScanner.Tests\PrivacyScanner.Tests.csproj
dotnet test PrivacyScanner.Infrastructure.Tests\PrivacyScanner.Infrastructure.Tests.csproj
```
 
> Note: only `PrivacyScanner.Tests` currently runs in CI; `PrivacyScanner.Infrastructure.Tests` must be
> run manually for now.
 
---
 
## Extending PrivacyScanner: Adding More Regex Rules
 
There are two ways to add detection rules, depending on whether it should apply only to your own
installation or ship as a built-in default for everyone.
 
### Option A — Add a rule via the UI (no code, no rebuild)
 
1. Click **"Regel hinzufügen"** (Add rule) on the main window, or open the settings (gear icon) to
   manage existing rules.
2. Provide a rule name and a **.NET regular expression** pattern.
3. Save. The rule is written to `%ProgramData%\PrivacyScanner\rules.json` and is immediately available
   for scanning, and can be toggled on/off, edited, or deleted at any time from the same UI.
 
This is the right option for a one-off or organization-specific pattern that doesn't need to be shipped
in the source code.
 
### Option B — Add a built-in default rule (developers)
 
Built-in rules ship with the application and are defined in
[`PrivacyScanner.Infrastructure/Scanner/Services/DefaultRegexRulesProvider.cs`](PrivacyScanner.Infrastructure/Scanner/Services/DefaultRegexRulesProvider.cs).
To add one, append a new entry to `GetDefaultRules()`:
 
```csharp
new RegexRuleDto
{
    RuleId = new Guid("00000001-0000-0000-0000-000000000009"), // next free ID in the sequence
    RuleName = "Sozialversicherungsnummer (DE)",
    Rule = @"\b\d{2}[ ]?\d{6}[ ]?[A-Z][ ]?\d{3}\b"
}
```
 
Important conventions to follow:
 
* **`RuleId` must be a fixed, unique GUID that never changes once released.** User installations merge
  their saved rule states with the built-in list *by `RuleId`* (`GetAllRegexRulesQueryHandler`), so a
  changed ID would silently disconnect a user's existing enable/disable choice for that rule from the
  rule itself. Use the next unused value in the existing `00000001-0000-0000-0000-0000000000XX`
  sequence.
* `Rule` is a standard **.NET regex** (`System.Text.RegularExpressions`), matched **per line** — the
  scanner reads files line by line, so patterns spanning multiple lines will not match.
* Keep new patterns as specific as reasonably possible. Several of the existing default rules (postal
  code, ID card number, address) are intentionally broad and are known to produce false positives — see
  [Known Limitations](#known-limitations). Avoid adding equally broad patterns without good reason.
* There is currently no regex timeout enforced, so avoid patterns prone to catastrophic backtracking
  (e.g. nested quantifiers) if the rule may run against large or adversarial input files.
* Add the new rule to a test in `PrivacyScanner.Infrastructure.Tests` (see `EmailRegexRuleTests.cs` for
  the pattern to follow: enabled by default, matches a valid example, doesn't match plain text).
 
The currently shipped default rules are:
 
| Rule | Detects |
|---|---|
| E-Mail-Adresse | Email addresses |
| IBAN | IBAN numbers |
| Telefonnummer (DE) | German phone numbers |
| Postleitzahl (DE) | German postal codes (any 5-digit number) |
| Personalausweisnummer (DE) | German ID card numbers (any 9-char alphanumeric token) |
| Kreditkartennummer | Visa/Mastercard/AMEX/Discover card numbers |
| Datum | Dates (`dd.mm.yyyy`, `yyyy-mm-dd`) |
| Adresse (Straße + Hausnummer) | Simplified German street + house number pattern |
 
---
 
## Requirements
 
* **To run the published executable:** Windows 10/11 x64. No .NET runtime install needed (self-contained
  single-file build).
* **To build from source:** .NET 10 SDK, Windows 10/11 (WPF UI).
* **Optional, for SpaCy/NLP detection only:**
 
  * Python 3.12 (explicitly required)
  * SpaCy for Python 3.12
  * German language model for SpaCy (`de_core_news_sm`)
 
Detailed SpaCy setup instructions can be found in [Install Instructions](install.md).
 
---
 
## Known Limitations
 
* SpaCy-based detection only supports **German** text (`de_core_news_sm`); other languages are not
  recognized.
* The default **postal code**, **ID card number**, and **address** regex rules are intentionally broad
  and can produce a significant number of false positives.
* The Python/SpaCy environment check and invocation rely on the Windows `py` launcher defaulting to
  Python 3.12; having multiple Python versions installed can cause the check to fail even if 3.12 is
  present (see [Troubleshooting in install.md](install.md#4-troubleshooting)).
* User-defined regex rules run without a match timeout.
 
---
 
## Project Structure
 
```text
PrivacyScanner/
├── PrivacyScanner.Model                       # DTOs / enums shared across layers
├── PrivacyScanner.Infrastructure.Contracts     # Mediator commands/queries and service contracts
├── PrivacyScanner.Infrastructure                # Command/query handlers, scan engine, SpaCy bridge
├── PrivacyScanner.UI                           # WPF (MVVM) application
├── PrivacyScanner.Tests                        # xUnit tests (run in CI)
├── PrivacyScanner.Infrastructure.Tests         # xUnit tests (not yet run in CI)
├── .github/workflows/pipeline.yml              # Build, format check, tests, publish
├── .editorconfig                               # Code style, enforced by Visual Studio/dotnet format
├── install.md                                  # Optional SpaCy setup instructions
├── PrivacyScanner.sln
```
 
---
 
## License
 
Apache License 2.0
