# -*- coding: utf-8 -*-
"""Arayüz çevirilerini (locales/tr.json, en.json) birleştirir. Kullanım: python add_i18n.py <json> [--overwrite]
JSON: {"tr": {...iç içe anahtarlar...}, "en": {...}}. Var olan anahtar --overwrite verilmezse korunur."""
import io
import json
import os
import sys

LOC = os.path.join(os.path.dirname(__file__), "..", "..", "frontend", "app", "locales")
src = json.load(io.open(sys.argv[1], encoding="utf-8"))
overwrite = "--overwrite" in sys.argv


def merge(dst, add, path=""):
    added = changed = 0
    for k, v in add.items():
        if isinstance(v, dict):
            node = dst.setdefault(k, {})
            if not isinstance(node, dict):
                raise SystemExit(f"type clash at {path}{k}")
            a, c = merge(node, v, f"{path}{k}.")
            added += a
            changed += c
        elif k not in dst:
            dst[k] = v
            added += 1
        elif dst[k] != v and overwrite:
            dst[k] = v
            changed += 1
    return added, changed


for lang in ("tr", "en"):
    if lang not in src:
        continue
    p = os.path.join(LOC, f"{lang}.json")
    d = json.load(io.open(p, encoding="utf-8"))
    a, c = merge(d, src[lang])
    io.open(p, "w", encoding="utf-8", newline="\n").write(json.dumps(d, ensure_ascii=False, indent=2) + "\n")
    print(lang, "added", a, "changed", c)
