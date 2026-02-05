# PrivacyScanner
 
PrivacyScanner is a .NET tool for scanning files for sensitive and personal data.
Detection is done through **configurable regex patterns** and optionally via **SpaCy** (Natural Language Processing).
 
---
 
## Overview
 
PrivacyScanner searches files for defined patterns (regexes) to identify potentially sensitive content.
Additionally, SpaCy can optionally be used to automatically detect personal data such as names, locations, or organizations.
 
---
 
## Features
 
* Scan files for sensitive information
* Support for custom regex patterns
* Optional .csv export
* Optional SpaCy integration for detecting personal data
 
---
 
## Project Structure
 
```text
PrivacyScanner/
├── PrivacyScanner.Infrastructure.Interfaces
├── PrivacyScanner.Infrastructure
├── PrivacyScanner.Model
├── PrivacyScanner.UI
├── PrivacyScanner.Tests
├── install.md
├── PrivacyScanner.sln
```
 
---
 
## Requirements
 
* If SpaCy is to be used:
 
  * Python 3.12 (explicitly required)
  * SpaCy for Python 3.12
  * Language package for SpaCy
 
Detailed instructions can be found in [Install Instructions](install.md).
 
---
 
## License
 
Apache License 2.0
