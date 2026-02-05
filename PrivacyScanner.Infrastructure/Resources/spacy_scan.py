import json
import sys
import argparse
import spacy
from pathlib import Path


def main():
    parser = argparse.ArgumentParser(
        prog="Spacy CLI",
        description="Analyse text and return tokens"
    )
    parser.add_argument("filename")
    parser.add_argument("-c", "--chunk_size", type=int, default=100_000)
    args = parser.parse_args()

    run(
        path=args.filename,
        chunk_size=args.chunk_size
    )


def run(path, chunk_size):
    nlp = spacy.load("de_core_news_sm")
    nlp.max_length = 2_000_000

    source = Path(path)
    if not source.exists():
        print(f"Datei '{path}' nicht gefunden.")
        sys.exit(1)

    text = source.read_text("utf-8")
    lines = text.splitlines(keepends=True)

    # Start-Offset jeder Zeile im Gesamtdokument
    line_offsets = []
    offset = 0
    for line in lines:
        line_offsets.append(offset)
        offset += len(line)

    def find_line_index(char_pos):
        # Rückwärts suchen ist hier ok, da Treffer meist nahe beieinander liegen
        for i in range(len(line_offsets) - 1, -1, -1):
            if char_pos >= line_offsets[i]:
                return i
        return 0

    entities = []

    buffer = ""
    buffer_start_offset = 0  # absolute Position im Gesamttest

    for line in lines:
        # Wenn der Buffer zu groß wird → analysieren
        if len(buffer) + len(line) > chunk_size:
            doc = nlp(buffer)

            for e in doc.ents:
                start = e.start_char + buffer_start_offset
                end = e.end_char + buffer_start_offset

                line_idx = find_line_index(start)
                line_start_offset = line_offsets[line_idx]

                entities.append({
                    "text": e.text,
                    "label": e.label_,
                    "hit_line_position": line_idx,
                    "start": start - line_start_offset,
                    "end": end - line_start_offset,
                    "prev_line": lines[line_idx - 1].rstrip("\n") if line_idx > 0 else None,
                    "hit_line": lines[line_idx].rstrip("\n"),
                    "next_line": lines[line_idx + 1].rstrip("\n") if line_idx + 1 < len(lines) else None
                })

            buffer_start_offset += len(buffer)
            buffer = ""

        buffer += line

    # Restbuffer verarbeiten
    if buffer:
        doc = nlp(buffer)

        for e in doc.ents:
            start = e.start_char + buffer_start_offset
            end = e.end_char + buffer_start_offset

            line_idx = find_line_index(start)
            line_start_offset = line_offsets[line_idx]

            entities.append({
                "text": e.text,
                "label": e.label_,
                "hit_line_position": line_idx + 1,
                "start": start - line_start_offset,
                "end": end - line_start_offset,
                "prev_line": lines[line_idx - 1].rstrip("\n") if line_idx > 0 else None,
                "hit_line": lines[line_idx].rstrip("\n"),
                "next_line": lines[line_idx + 1].rstrip("\n") if line_idx + 1 < len(lines) else None
            })

    result = {"entities": entities}
    print(json.dumps(result, ensure_ascii=False))


if __name__ == "__main__":
    main()

