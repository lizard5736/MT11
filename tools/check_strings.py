#!/usr/bin/env python3
"""Fails when the app references a localization key the string table lacks.

Scans src/Grip for L.S("…"), L.F("…"), Localizer[…], {l:T …} and the
key-building conventions (feature.*, group.*, energy.*, action.*), then
compares against the keys defined in src/Grip.Core/Localization.
Usage: python3 tools/check_strings.py
"""
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(ROOT, "src", "Grip")
TABLE = os.path.join(ROOT, "src", "Grip.Core", "Localization")

defined = set()
for name in os.listdir(TABLE):
    if name.startswith("StringTable") and name.endswith(".cs"):
        text = open(os.path.join(TABLE, name), encoding="utf-8").read()
        defined.update(re.findall(r'\bA\("([^"]+)"', text))
        # F("id", ...) expands to feature.id.title / feature.id.desc
        for fid in re.findall(r'\bF\("([^"]+)"', text):
            defined.add(f"feature.{fid}.title")
            defined.add(f"feature.{fid}.desc")

patterns = [
    r'\bL\.(?:S|F|Count)\("([^"{}]+)"',
    r'Localizer\.Instance\["([^"]+)"\]',
    r'\bloc\["([^"]+)"\]',
    r'\bloc\.(?:Format|Count|Plural)\("([^"]+)"',
    r'\{l:T ([A-Za-z0-9_.]+)',
    r'"(@[a-z][A-Za-z0-9_.]+)"',
]
used = {}
for folder, _, files in os.walk(APP):
    if os.sep + "obj" in folder or os.sep + "bin" in folder:
        continue
    for name in files:
        if not name.endswith((".cs", ".xaml")):
            continue
        path = os.path.join(folder, name)
        text = open(path, encoding="utf-8").read()
        for pattern in patterns:
            for key in re.findall(pattern, text):
                key = key.lstrip("@")
                used.setdefault(key, set()).add(os.path.relpath(path, ROOT))

# Core also builds keys for radial defaults.
core_radial = open(os.path.join(ROOT, "src", "Grip.Core", "Radial", "RadialMenu.cs"), encoding="utf-8").read()
for key in re.findall(r'"@([a-z][A-Za-z0-9_.]+)"', core_radial):
    used.setdefault(key, set()).add("src/Grip.Core/Radial/RadialMenu.cs")

# Keys passed as literals to page builders and helpers (b.Toggle("settings.x", ...)).
PREFIXES = ("settings|general|access|hotkeys|about|hub|keepAwake|clipboard|feature|toggle|url|onboarding|panel|"
            "controls|common|action|group|energy|radial|commandBar|shelf|utilities|preset|time|error|tray|app")
literal = re.compile(r'"((?:' + PREFIXES + r')\.[A-Za-z0-9_.]+)"')
for folder, _, files in os.walk(os.path.join(APP, "UI")):
    for name in files:
        if name.endswith(".cs"):
            path = os.path.join(folder, name)
            for key in literal.findall(open(path, encoding="utf-8").read()):
                used.setdefault(key, set()).add(os.path.relpath(path, ROOT))

# Dynamic prefixes ("energy." + value) are covered by the Core unit tests.
used = {k: v for k, v in used.items()
        if not k.endswith(".") and k != "key" and not k.endswith((".png", ".json", ".exe", ".txt"))}
missing = {k: v for k, v in used.items() if k not in defined}
if missing:
    print("Missing string keys:")
    for key, files in sorted(missing.items()):
        print(f"  {key}    ({', '.join(sorted(files))})")
    sys.exit(1)
print(f"ok: {len(used)} keys used, {len(defined)} defined")
