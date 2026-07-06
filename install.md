# Requirements for Starting the Application

This guide covers the **optional SpaCy/NLP detection feature**. PrivacyScanner runs and scans with its
built-in regex rules without any of this installed. These steps are only required if you want to enable
the "Use SpaCy" checkbox in the application to additionally detect names, locations, and organizations
in free text.

> **Why the exact steps matter:** PrivacyScanner invokes Python through the Windows `py` launcher
> (`py --version`, `py -3.12 -m ...`), not the plain `python` command. If more than one Python version
> is installed, `py` may resolve to a different (newer) version by default, and packages installed via
> a bare `python -m pip install ...` may land in a different interpreter than the one the app actually
> uses. Following the exact commands below (and keeping only Python 3.12 installed) avoids this
> mismatch.

## 1. Required Python Version

**Requirement:**
The application explicitly requires Python version **3.12.x**. Other versions will cause errors, and
having an *additional*, newer Python version installed alongside 3.12 can also cause the check below to
fail, because the `py` launcher defaults to the newest installed version unless only one is present.

**Installation:**
Python can be downloaded here:
[Python 3.12.0 Download](https://www.python.org/downloads/release/python-3120/)

**Important during installation:**

* ✅ Enable "Add Python to PATH"
* ✅ Install Python Launcher (py)

**Verification:**

```bash
py --version
```

**Result:**

* Correct:

```
Python 3.12.0
```

* Incorrect:

```
Python 3.14.0
```

**Correction:**
If the wrong (or an additional) Python version is installed, it must be uninstalled so Python 3.12 is
the only version on the machine, then `py --version` must be re-checked.

**Uninstallation:**

1. Open Settings: `Apps → Installed Apps`
2. Search for Python
3. Select entries like `Python 3.14.0 (64-bit)`
4. Click **Uninstall**

---

## 2. Required Python Packages

**spaCy**
The application uses **spaCy** for language-based analysis.

**Minimum requirement:**

* spaCy installed for Python 3.12

**Installation:**

```bash
py -3.12 -m pip install -U spacy
```

> Use exactly this command (with `py -3.12`), not `python -m pip install`, so that spaCy is installed
> into the same Python 3.12 environment that PrivacyScanner uses at runtime.

---

## 3. Required spaCy Language Model

**German language model:** `de_core_news_sm`
This model is required for processing German texts. SpaCy detection is currently **German-only** —
other languages are not supported by the shipped model.

**Model installation:**

```bash
py -3.12 -m spacy download de_core_news_sm
```

---

## 4. Troubleshooting

PrivacyScanner runs an environment check on startup and shows one of these messages if something is
missing. Match the message to the fix:

| Message in the app | Cause | Fix |
|---|---|---|
| "Python 3.12 konnte nicht gefunden werden." | `py` is not on PATH, or no Python is installed | Reinstall Python 3.12 with "Add Python to PATH" and "Install Python Launcher" enabled |
| "Die installierte Python-Version konnte nicht korrekt erkannt werden." | `py --version` resolves to a version other than 3.12 (e.g. a newer Python is also installed) | Uninstall the other Python version(s), keep only 3.12 |
| "spaCy ist nicht installiert." | spaCy wasn't installed into the Python 3.12 environment | Run `py -3.12 -m pip install -U spacy` |
| "Das spaCy Sprachmodell 'de_core_news_sm' ist nicht installiert." | Language model missing | Run `py -3.12 -m spacy download de_core_news_sm` |

If SpaCy setup fails or is skipped entirely, the application still works normally using only the
regex-based detection.
