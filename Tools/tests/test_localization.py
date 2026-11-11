import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('localization', Path(__file__).parents[1] / 'localization.py')
loc = importlib.util.module_from_spec(spec)
spec.loader.exec_module(loc)

class TranslationJobs(unittest.TestCase):
    def setUp(self):
        self.temp = tempfile.TemporaryDirectory()
        self.addCleanup(self.temp.cleanup)
        self.root = Path(self.temp.name)
        self.source = '完成 {0} 项。'
        self.catalog = {'ui.done': {'Source': self.source, 'SourceHash': loc.digest(self.source), 'Contexts': ['test'], 'RawArgs': [True]}}
        for name, value in [('catalog', self.catalog), ('zh-CN', {'ui.done': self.source}), ('en-US', {}), ('languages', [{'Code': 'zh-CN'}, {'Code': 'en-US'}]), ('translation-state', {})]:
            loc.write(self.root / (name + '.json'), value)
        self.job = self.root / 'job.json'
        loc.export_job(self.root, 'en-US', self.job)
        self.data = loc.read(self.job)
        self.data['entries'][0]['target'] = '{0} items completed.'

    def test_valid_import_then_incremental_export(self):
        loc.write(self.job, self.data)
        loc.import_job(self.root, 'en-US', self.job)
        loc.check(self.root)
        loc.export_job(self.root, 'en-US', self.job)
        self.assertEqual([], loc.read(self.job)['entries'])

    def assert_rejected_without_writes(self):
        before = {p.name: p.read_bytes() for p in self.root.glob('*.json') if p != self.job}
        loc.write(self.job, self.data)
        with self.assertRaises(ValueError): loc.import_job(self.root, 'en-US', self.job)
        self.assertEqual(before, {p.name: p.read_bytes() for p in self.root.glob('*.json') if p != self.job})

    def test_placeholder_rejection(self):
        self.data['entries'][0]['target'] = 'Done'
        self.assert_rejected_without_writes()

    def test_stale_source_rejection(self):
        loc.write(self.root / 'zh-CN.json', {'ui.done': '已处理 {0} 项。'})
        self.assert_rejected_without_writes()

    def test_unknown_key_rejection(self):
        self.data['entries'][0]['key'] = 'ui.unknown'
        self.assert_rejected_without_writes()

    def test_duplicate_job_key_rejection(self):
        self.data['entries'].append(dict(self.data['entries'][0]))
        self.assert_rejected_without_writes()

    def test_batch_rejection(self):
        self.data['entries'].append({'key': 'bad', 'target': 'Bad'})
        self.assert_rejected_without_writes()

    def test_invalid_format_rejection(self):
        self.data['entries'][0]['target'] = '{0} {'
        self.assert_rejected_without_writes()

    def test_duplicate_json_rejection(self):
        self.job.write_text('{"schema":1,"schema":1}', encoding='utf-8')
        with self.assertRaises(ValueError): loc.read(self.job)

    def test_windows_utf8_bom_job_is_accepted(self):
        self.job.write_text(json.dumps(self.data, ensure_ascii=False), encoding='utf-8-sig')
        loc.import_job(self.root, 'en-US', self.job)
        self.assertEqual('{0} items completed.', loc.read(self.root / 'en-US.json')['ui.done'])

    def test_tokens_and_file_filters(self):
        with self.assertRaises(ValueError): loc.validate('link', 'See another site', {'ProtectedTokens': ['https://example.org']}, '查看 https://example.org')
        with self.assertRaises(ValueError): loc.validate('filter', 'XML|*.exe', {'FilterPatterns': ['*.xml']}, 'XML|*.xml')
        with self.assertRaises(ValueError): loc.validate('code', 'Changed', {'Opaque': True, 'Source': '原文'}, '原文')

    def test_summary_layout(self):
        with self.assertRaises(ValueError):
            loc.validate('summary', 'A: {0} B: {1}', {'Source': '甲：{0}\n乙：{1}', 'StructuredLines': True}, '甲：{0}\n乙：{1}')

    def test_language_path_rejection(self):
        with self.assertRaises(ValueError): loc.language(self.root, '../en-US')

    def test_add_stable_key_requires_translation(self):
        loc.add(self.root, 'ui.next', '下一步', 'Desktop/Example.xaml:Text')
        loc.export_job(self.root, 'en-US', self.job)
        self.assertEqual({'ui.done', 'ui.next'}, {item['key'] for item in loc.read(self.job)['entries']})
        with self.assertRaises(ValueError): loc.add(self.root, 'ui.next', '下一步', 'test')

    def test_new_language_can_be_translated_in_batches(self):
        loc.init_language(self.root, 'zh-Hant', '繁體中文')
        loc.add(self.root, 'ui.cancel', '取消', 'Desktop/Test.xaml')
        loc.export_job(self.root, 'zh-Hant', self.job)
        job = loc.read(self.job)
        job['entries'][0]['target'] = '已完成 {0} 項。'
        loc.write(self.job, job)
        loc.import_job(self.root, 'zh-Hant', self.job, translated_only=True)
        self.assertEqual({'ui.done': '已完成 {0} 項。'}, loc.read(self.root / 'zh-Hant.json'))
        self.assertNotIn('ui.cancel', loc.read(self.root / 'translation-state.json')['zh-Hant'])
        loc.export_job(self.root, 'zh-Hant', self.job)
        self.assertEqual(['ui.cancel'], [item['key'] for item in loc.read(self.job)['entries']])

    def test_initialization_rejects_existing_or_unsafe_language_without_writes(self):
        before = {p.name: p.read_bytes() for p in self.root.glob('*.json')}
        for code, name in [('en-US', 'English'), ('../escape', 'Bad'), ('zh-Hant', '  ')]:
            with self.assertRaises(ValueError): loc.init_language(self.root, code, name)
            self.assertEqual(before, {p.name: p.read_bytes() for p in self.root.glob('*.json')})

    def test_partial_import_still_rejects_invalid_completed_translations(self):
        self.data['entries'][0]['target'] = 'Missing placeholder'
        loc.write(self.job, self.data)
        before = (self.root / 'en-US.json').read_bytes()
        with self.assertRaises(ValueError): loc.import_job(self.root, 'en-US', self.job, translated_only=True)
        self.assertEqual(before, (self.root / 'en-US.json').read_bytes())

    def test_initialization_can_resume_after_an_interrupted_write(self):
        loc.write(self.root / 'zh-Hant.json', {})
        loc.init_language(self.root, 'zh-Hant', '繁體中文')
        self.assertIn('zh-Hant', [item['Code'] for item in loc.read(self.root / 'languages.json')])

    def test_technical_examples_are_prefilled_without_translation(self):
        catalog = loc.read(self.root / 'catalog.json')
        catalog['ui.done']['Opaque'] = True
        loc.write(self.root / 'catalog.json', catalog)
        loc.export_job(self.root, 'en-US', self.job)
        self.assertEqual(self.source, loc.read(self.job)['entries'][0]['target'])

if __name__ == '__main__': unittest.main()
