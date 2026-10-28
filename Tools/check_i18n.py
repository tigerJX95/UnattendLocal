#!/usr/bin/env python3
"""Static validation of localization JSON files. Python stdlib only."""
import hashlib
import json
import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

base = Path(__file__).resolve().parents[1] / 'Localization' / 'Resources'
errors = []
placeholder = re.compile(r'(?<!\{)\{\d+(?:,[^{}]+)?(?::[^{}]+)?\}(?!\})')

def load(name):
    path = base / name
    try:
        def unique(pairs):
            data = {}
            for key, val in pairs:
                if key in data: errors.append(f'{name}: duplicate key {key}')
                data[key] = val
            return data
        return json.loads(path.read_text(encoding='utf-8-sig'), object_pairs_hook=unique)
    except (OSError, UnicodeError, ValueError) as exc:
        errors.append(f'{name}: invalid JSON/UTF-8: {exc}')
        return None

catalog, manifest, state, zh = (load(n) for n in ('catalog.json', 'languages.json', 'translation-state.json', 'zh-CN.json'))
if not isinstance(catalog, dict) or not isinstance(manifest, list) or not isinstance(state, dict) or not isinstance(zh, dict):
    sys.exit('Required i18n files invalid; ' + '; '.join(errors))
keys = set(catalog)
codes = []
for item in manifest:
    if not isinstance(item, dict): errors.append('Invalid language item'); continue
    code, name = item.get('Code'), item.get('Name')
    if not isinstance(code, str) or not re.fullmatch(r'[a-z]{2,3}(?:-[A-Za-z0-9]{2,8})*', code) or not isinstance(name, str) or not name.strip() or len(name)>80:
        errors.append(f'Invalid language identity: {item}'); continue
    if code in codes: errors.append(f'Duplicate language: {code}')
    codes.append(code)
if not {'zh-CN','en-US'}.issubset(codes) or len(codes)>64: errors.append('Required language codes absent or too many languages')
for key, entry in catalog.items():
    if not isinstance(entry, dict) or not isinstance(entry.get('Source'), str):
        errors.append(f'Invalid catalog entry: {key}'); continue
    if hashlib.sha256(entry['Source'].encode()).hexdigest() != entry.get('SourceHash'):
        errors.append(f'Catalog SourceHash mismatch: {key}')
for code in codes:
    data = zh if code=='zh-CN' else load(f'{code}.json')
    if not isinstance(data, dict): errors.append(f'{code}: invalid resource object'); continue
    missing, extra = keys - data.keys(), data.keys() - keys
    if missing and code in ('zh-CN','en-US'): errors.append(f'{code}: {len(missing)} missing keys, e.g. {sorted(missing)[:5]}')
    if extra: errors.append(f'{code}: {len(extra)} unknown keys, e.g. {sorted(extra)[:5]}')
    for key in keys & data.keys():
        value, source, entry = data[key], zh.get(key), catalog[key]
        if not isinstance(value,str) or not value.strip(): errors.append(f'{code}: empty/non-string: {key}'); continue
        if not isinstance(source,str) or not isinstance(entry,dict): continue
        if sorted(placeholder.findall(value)) != sorted(placeholder.findall(source)):
            errors.append(f'{code}: placeholders differ: {key}')
        if entry.get('Opaque') and value != entry['Source']: errors.append(f'{code}: opaque resource modified: {key}')
        if code == 'en-US' and not entry.get('Opaque') and value != '简体中文' and re.search(r'[\u4e00-\u9fff]', value):
            errors.append(f'{code}: untranslated Chinese text: {key}')
        if any(t not in value for t in (entry.get('ProtectedTokens') or [])):
            errors.append(f'{code}: protected token missing: {key}')
        patterns = entry.get('FilterPatterns')
        if patterns is not None and value.split('|')[1::2] != patterns:
            errors.append(f'{code}: filter pattern modified: {key}')
        if entry.get('StructuredLines'):
            original, translated = entry['Source'].split('\n'), value.split('\n')
            if len(original)!=len(translated): errors.append(f'{code}: structured line count: {key}')
            else:
                for i,(left,right) in enumerate(zip(original,translated),1):
                    if sorted(placeholder.findall(left))!=sorted(placeholder.findall(right)) or ('：' in left and ':' not in right and '：' not in right):
                        errors.append(f'{code}: structured line {i}: {key}')
    print(f'{code}: {len(keys & data.keys())}/{len(keys)} translated keys ({len(keys & data.keys())/max(len(keys),1):.1%})')
    if code=='en-US':
        review=state.get(code)
        if not isinstance(review,dict) or set(review)!=keys: errors.append('translation-state: en-US review set differs')
        else:
            for key in keys & zh.keys():
                if review[key]!=hashlib.sha256(zh[key].encode()).hexdigest():
                    errors.append(f'translation-state: stale review source: {key}')
# Source identities are valid for model choices and icon buttons, but explicit
# tooltip text must opt into presentation localization rather than display Source.
wpf = '{http://schemas.microsoft.com/winfx/2006/xaml/presentation}'
button_count = tooltip_count = 0
for path in sorted((base.parents[1] / 'Desktop').rglob('*.xaml')):
    try:
        elements = ET.parse(path).getroot()
    except ET.ParseError as exc:
        errors.append(f'{path.name}: invalid XAML: {exc}')
        continue
    for element in elements.iter():
        if element.tag in (wpf + 'Button', wpf + 'ToggleButton'):
            button_count += 1
            if re.search(r'[\u4e00-\u9fff]', element.get('Content', '')):
                errors.append(f'{path.name}: untranslated button label')
        if element.tag == wpf + 'ToolTip':
            tooltip_count += 1
            for child in element.iter():
                for attribute in ('Content', 'Text'):
                    value = child.get(attribute, '')
                    if value.startswith('{loc:Source ') or re.search(r'[\u4e00-\u9fff]', value):
                        errors.append(f'{path.name}: tooltip bypasses display localization: {value}')
print(f'XAML presentation: {button_count} buttons, {tooltip_count} explicit tooltips checked')
for e in errors[:50]: print('ERROR:',e,file=sys.stderr)
if len(errors)>50: print(f'...and {len(errors)-50} more',file=sys.stderr)
print(f'i18n check: {"FAIL" if errors else "PASS"} ({len(errors)} issues)')
sys.exit(bool(errors))
