# Requirements for Starting the Application

## 1. Required Python Version

**Requirement:**
The application explicitly requires Python version **3.12.x**. Other versions will cause errors.

**Installation:**
Python can be downloaded here:
[Python 3.12.0 Download](https://www.python.org/downloads/release/python-3120/)

**Important during installation:**

* ✅ Enable "Add Python to PATH"
* ✅ Install Python Launcher (py)

**Verification:**

```bash
python --version
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
If the wrong Python version is installed, it must be uninstalled and the correct version must be installed.

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
python -m pip install -U spacy
```

---

## 3. Required spaCy Language Model

**German language model:** `de_core_news_sm`
This model is required for processing German texts.

**Model installation:**

```bash
python -m spacy download de_core_news_sm
```
