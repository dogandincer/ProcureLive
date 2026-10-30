# -*- coding: utf-8 -*-
"""Sunucu mesaj çevirisi (MessageTranslator) ekler. Kullanım: python add_tr.py <json>
JSON: {"static": [[tr, en], ...], "regex": [[csharp_verbatim_pattern, csharp_render_expression], ...]}"""
import io
import json
import os
import sys

P = os.path.join(os.path.dirname(__file__), "..", "..", "backend", "src", "Application", "Common", "Localization", "MessageTranslator.cs")
data = json.load(io.open(sys.argv[1], encoding="utf-8"))
s = io.open(P, encoding="utf-8").read()

anchor = '        ["Kapanmış sürecin işyeri değiştirilemez."] = "The workspace of a closed process cannot be changed.",\n'
assert anchor in s
add = ""
for tr, en in data.get("static", []):
    key = '["' + tr.replace('"', '\\"') + '"]'
    if key in s:
        continue
    add += f'        {key} = "{en.replace(chr(34), chr(92) + chr(34))}",\n'
s = s.replace(anchor, anchor + add, 1)

end_anchor = "    ];\n\n    private static string RenderInvitationResponse"
assert end_anchor in s
radd = ""
for pattern, render in data.get("regex", []):
    if pattern in s:
        continue
    radd += f'        (new Regex(@"{pattern}"),\n            m => {render}),\n'
s = s.replace(end_anchor, radd + end_anchor, 1)
io.open(P, "w", encoding="utf-8", newline="").write(s)
print("static", add.count("\n"), "regex", radd.count("new Regex"))
