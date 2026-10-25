#!/usr/bin/env python3
"""Provider-neutral translation jobs. Python 3.10+, standard library only."""
import argparse
from collections import Counter
import hashlib
import json
import os
from pathlib import Path
import re
import tempfile

DEFAULT = Path(__file__).resolve().parents[1] / 'Localization' / 'Resources'
PLACEHOLDER = re.compile(r'(?<!\{)\{\d+(?:,[^{}]+)?(?::[^{}]+)?\}(?!\})')
URL = re.compile(r'https?://[^\s<>"\)]+')

def digest(value):
    return hashlib.sha256(value.encode('utf-8')).hexdigest()

def unique(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError('Duplicate JSON key: ' + key)
        result[key] = value
    return result

def read(path):
    return json.loads(Path(path).read_text(encoding='utf-8'), object_pairs_hook=unique)

def write(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    fd, name = tempfile.mkstemp(dir=path.parent, prefix='.locale-', suffix='.tmp')
    try:
        with os.fdopen(fd, 'w', encoding='utf-8', newline='\n') as stream:
            json.dump(value, stream, ensure_ascii=False, indent=2)
            stream.write('\n')
        os.replace(name, path)
    finally:
        if os.path.exists(name):
            os.unlink(name)

def validate(key, value, entry, source):
    if not isinstance(value, str) or not value.strip():
        raise ValueError('Empty/non-string translation: ' + key)
    if Counter(PLACEHOLDER.findall(source)) != Counter(PLACEHOLDER.findall(value)):
        raise ValueError('Changed placeholders: ' + key)
    if entry.get('StructuredLines'):
        original_lines, target_lines = entry['Source'].split('\n'), value.split('\n')
        if len(original_lines) != len(target_lines):
            raise ValueError('Changed summary lines: ' + key)
        for original_line, target_line in zip(original_lines, target_lines):
            if Counter(PLACEHOLDER.findall(original_line)) != Counter(PLACEHOLDER.findall(target_line)):
                raise ValueError('Changed summary placeholders: ' + key)
            if '：' in original_line and not re.search('[:：]', target_line):
                raise ValueError('Missing summary label separator: ' + key)
    if entry.get('Opaque') and value != entry['Source']:
        raise ValueError('Changed technical example: ' + key)
    for token in entry.get('ProtectedTokens', []):
        if token not in value:
            raise ValueError('Changed protected token: ' + key + ': ' + token)
    if 'FilterPatterns' in entry and value.split('|')[1::2] != entry['FilterPatterns']:
        raise ValueError('Changed file filter: ' + key)
    # Braces outside numbered placeholders must remain balanced in templates.
    if PLACEHOLDER.search(source):
        rest = PLACEHOLDER.sub('', value).replace('{{', '').replace('}}', '')
        if '{' in rest or '}' in rest:
            raise ValueError('Invalid format template: ' + key)

def language(root, code):
    manifest = read(root / 'languages.json')
    if code not in [item['Code'] for item in manifest]:
        raise ValueError('Register language in languages.json first: ' + code)
    if not re.fullmatch(r'[a-z]{2,3}(?:-[A-Za-z0-9]{2,8})*', code):
        raise ValueError('Invalid language code')
    path = root / (code + '.json')
    return path, read(path) if path.exists() else {}

def load(root):
    return read(root / 'catalog.json'), read(root / 'zh-CN.json'), read(root / 'translation-state.json')

def check(root):
    catalog, source, state = load(root)
    if set(catalog) != set(source):
        raise ValueError('catalog.json and zh-CN.json keys differ')
    manifest = read(root / 'languages.json')
    codes = [item['Code'] for item in manifest]
    if len(codes) != len(set(codes)) or not {'zh-CN', 'en-US'} <= set(codes):
        raise ValueError('Duplicate/missing language codes')
    for key, entry in catalog.items():
        if entry['SourceHash'] != digest(entry['Source']):
            raise ValueError('Canonical source changed without migration: ' + key)
        validate(key, source[key], entry, entry['Source'])
    for code in codes:
        _, target = language(root, code)
        if set(target) - set(catalog):
            raise ValueError('Unknown translation keys: ' + code)
        if code == 'en-US' and set(target) != set(catalog):
            raise ValueError('English pack is incomplete')
        for key, value in target.items():
            validate(key, value, catalog[key], source[key])
            if code != 'zh-CN' and state.get(code, {}).get(key) != digest(source[key]):
                raise ValueError('Translation needs review: ' + code + ': ' + key)
            if code == 'en-US' and not catalog[key].get('Opaque') and re.search('[\u4e00-\u9fff]', value) and value != '简体中文':
                raise ValueError('Untranslated English entry: ' + key)
    print(f'Localization: {len(catalog)} keys, {len(codes)} languages validated.')

def export_job(root, code, path, all_entries=False):
    if code == 'zh-CN':
        raise ValueError('Jobs translate from zh-CN; edit the source pack directly')
    catalog, source, state = load(root)
    _, target = language(root, code)
    entries = []
    for key, entry in catalog.items():
        if all_entries or key not in target or state.get(code, {}).get(key) != digest(source[key]):
            entries.append({'key': key, 'source': source[key], 'source_hash': digest(source[key]),
                            'contexts': entry['Contexts'], 'protected_tokens': entry.get('ProtectedTokens', []),
                            'placeholders': PLACEHOLDER.findall(source[key]), 'opaque': entry.get('Opaque', False),
                            'target': target.get(key, '')})
    write(path, {'schema': 1, 'source_language': 'zh-CN', 'target_language': code, 'entries': entries})
    print(f'Exported {len(entries)} translation jobs.')

def import_job(root, code, path):
    catalog, source, state = load(root)
    target_path, target = language(root, code)
    if code == 'zh-CN':
        raise ValueError('Import target must differ from the source language')
    job = read(path)
    if job.get('schema') != 1 or job.get('source_language') != 'zh-CN' or job.get('target_language') != code:
        raise ValueError('Translation job language/schema mismatch')
    seen = set()
    for item in job['entries']:
        key = item['key']
        if key in seen or key not in catalog:
            raise ValueError('Duplicate/unknown job key: ' + key)
        seen.add(key)
        if item['source_hash'] != digest(source[key]) or item['source'] != source[key]:
            raise ValueError('Stale source: ' + key + '; export the job again')
        validate(key, item['target'], catalog[key], source[key])
        target[key] = item['target']
        state.setdefault(code, {})[key] = item['source_hash']
    # Validate the complete batch before changing either file. If a write is
    # interrupted, check/export detects stale state and requires review again.
    write(target_path, target)
    write(root / 'translation-state.json', state)
    print(f'Imported {len(seen)} translations.')

def add(root, key, text, context):
    catalog, source, _ = load(root)
    if key in catalog or not re.fullmatch('[A-Za-z][A-Za-z0-9_.-]+', key):
        raise ValueError('Key already exists or is invalid')
    catalog[key] = {'Source': text, 'SourceHash': digest(text), 'Contexts': [context],
                    'RawArgs': [], 'ProtectedTokens': URL.findall(text)}
    source[key] = text
    write(root / 'catalog.json', catalog)
    write(root / 'zh-CN.json', source)
    print('Key added. Reference it from XAML/C# and export a translation job.')

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--resources', type=Path, default=DEFAULT)
    sub = parser.add_subparsers(dest='command', required=True)
    sub.add_parser('check')
    for command in ('export', 'import'):
        p = sub.add_parser(command)
        p.add_argument('--language', required=True)
        p.add_argument('--file', type=Path, required=True)
        if command == 'export':
            p.add_argument('--all', action='store_true')
    p = sub.add_parser('add')
    p.add_argument('--key', required=True)
    p.add_argument('--source', required=True)
    p.add_argument('--context', required=True)
    args = parser.parse_args()
    try:
        if args.command == 'check': check(args.resources)
        elif args.command == 'export': export_job(args.resources, args.language, args.file, args.all)
        elif args.command == 'import': import_job(args.resources, args.language, args.file)
        else: add(args.resources, args.key, args.source, args.context)
    except (ValueError, KeyError, TypeError, OSError) as error:
        parser.exit(1, str(error) + '\n')

if __name__ == '__main__':
    main()
