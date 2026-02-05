# Anforderungen zum Start der Anwendung

## 1. Erforderliche Python-Version

**Anforderung:**
Die Anwendung braucht explizit die Python Version **3.12.x**. Andere Versionen führen zu Fehlern.

**Installation:**
Python kann hier heruntergeladen werden:
[Python 3.12.0 Download](https://www.python.org/downloads/release/python-3120/)

**Wichtig bei der Installation:**

* ✅ „Add Python to PATH“ aktivieren
* ✅ Python Launcher (py) mitinstallieren

**Überprüfung:**

```bash
python --version
```

**Ergebnis:**

* Richtig:

```
Python 3.12.0
```

* Falsch:

```
Python 3.14.0
```

**Korrektur:**
Ist die falsche Python-Version installiert, muss diese deinstalliert und die richtige Version installiert werden.

**Deinstallation:**

1. Einstellungen öffnen: `Apps → Installierte Apps`
2. Nach Python suchen
3. Einträge wie `Python 3.14.0 (64-bit)` auswählen
4. **Deinstallieren** klicken

---

## 2. Erforderliche Python-Pakete

**spaCy**
Die Anwendung nutzt **spaCy** für sprachbasierte Analysen.

**Mindestanforderung:**

* spaCy installiert für Python 3.12

**Installation:**

```bash
python -m pip install -U spacy
```

---

## 3. Erforderliches spaCy-Sprachmodell

**Deutsches Sprachmodell:** `de_core_news_sm`
Dieses Modell wird für die Verarbeitung deutscher Texte benötigt.

**Installation des Modells:**

```bash
python -m spacy download de_core_news_sm
```
