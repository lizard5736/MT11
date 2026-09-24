#!/usr/bin/env python3
"""Lints WPF resource keys in src/Grip.

* every {StaticResource X} / {DynamicResource X}, FindResource("X") and
  SetResourceReference(..., "X") must point at a key that exists;
* a key must not be defined twice across the app-level dictionaries (a
  Style and a Brush sharing a name silently shadow each other).
Usage: python3 tools/check_resources.py
"""
import os
import re
import sys
from collections import defaultdict

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
APP = os.path.join(ROOT, "src", "Grip")
THEME = os.path.join(APP, "UI", "Theme")

definitions = defaultdict(list)
for name in os.listdir(THEME):
    if name.endswith(".xaml"):
        text = open(os.path.join(THEME, name), encoding="utf-8").read()
        for key in re.findall(r'x:Key="([^"]+)"', text):
            definitions[key].append(name)

# Palettes define the same keys on purpose (they are swapped, never merged together).
problems = []
for key, files in definitions.items():
    files = [f for f in files if not f.startswith("Colors.")] + [f for f in files if f.startswith("Colors.")][:1]
    if len(files) > 1:
        problems.append(f"duplicate key {key} in {files}")

local_keys = set()
references = []
for folder, _, files in os.walk(APP):
    if os.sep + "obj" in folder or os.sep + "bin" in folder:
        continue
    for name in files:
        path = os.path.join(folder, name)
        rel = os.path.relpath(path, ROOT)
        if name.endswith(".xaml"):
            text = open(path, encoding="utf-8").read()
            local_keys.update(re.findall(r'x:Key="([^"]+)"', text))
            for key in re.findall(r'\{(?:Static|Dynamic)Resource ([A-Za-z0-9_.]+)\}', text):
                references.append((key, rel))
        elif name.endswith(".cs"):
            text = open(path, encoding="utf-8").read()
            local_keys.update(re.findall(r'Resources\["([^"]+)"\]\s*=', text))
            for key in re.findall(r'(?:FindResource|TryFindResource)\("([^"]+)"\)', text):
                references.append((key, rel))
            for key in re.findall(r'SetResourceReference\([^,]+,\s*"([^"]+)"\)', text):
                references.append((key, rel))
            for key in re.findall(r'SetResourceReference\([^,]+,\s*[^?"]+\?\s*"([^"]+)"\s*:\s*"([^"]+)"\)', text):
                for k in key:
                    references.append((k, rel))

known = set(definitions) | local_keys
for key, rel in references:
    if key.startswith("Icon."):
        continue
    if key not in known:
        problems.append(f"missing resource {key} (used in {rel})")

if problems:
    print("\n".join(sorted(set(problems))))
    sys.exit(1)
print(f"ok: {len(known)} keys, {len(references)} references")
