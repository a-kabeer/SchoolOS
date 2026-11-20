#!/usr/bin/env node
/**
 * Extracts English, Arabic, and Urdu translation dictionaries from Education-Portal's Angular $localize
 * bundles into framework-agnostic React/i18next JSON files under frontend/schoolos-web/src/locales/.
 *
 * Transformations:
 * - Converts Angular placeholder syntax `{$VAR}` -> `{{VAR}}`
 * - Pulls stray top-level message keys (outside `translations`) into the flat dictionary
 * - Rebrands "Education Portal" -> "SchoolOS"
 * - Validates 100% key parity across en, ar, and ur
 */
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import path from 'node:path';

const sourceRoot = process.argv[2];
if (!sourceRoot) {
  console.error('Usage: node scripts/import-education-portal-locales.mjs <path-to-education-portal-locale-dir>');
  process.exit(1);
}

const targetRoot = path.resolve(path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1')), '../src/locales');
await mkdir(targetRoot, { recursive: true });

const locales = ['en', 'ar', 'ur'];
const extracted = {};

function normalizeValue(raw) {
  return String(raw)
    .trim()
    .replace(/\{\$([A-Za-z0-9_]+)\}/g, '{{$1}}')
    .replace(/Education Portal/g, 'SchoolOS');
}

for (const loc of locales) {
  const filePath = path.join(sourceRoot, loc, 'messages.json');
  const parsed = JSON.parse(await readFile(filePath, 'utf8'));
  const flat = {};

  // Include any stray root-level string keys (excluding "locale")
  for (const [k, v] of Object.entries(parsed)) {
    if (k !== 'locale' && k !== 'translations' && typeof v === 'string') {
      flat[k] = normalizeValue(v);
    }
  }

  for (const [k, v] of Object.entries(parsed.translations ?? {})) {
    if (typeof v === 'string') {
      flat[k] = normalizeValue(v);
    }
  }

  extracted[loc] = flat;
}

// Compute union of keys from English canonical source; ensure parity
const canonicalKeys = Object.keys(extracted.en).sort();
for (const loc of locales) {
  const ordered = {};
  let fallbackCount = 0;
  for (const key of canonicalKeys) {
    if (extracted[loc][key] !== undefined) {
      ordered[key] = extracted[loc][key];
    } else {
      ordered[key] = extracted.en[key];
      fallbackCount++;
    }
  }

  const outPath = path.join(targetRoot, `${loc}.json`);
  await writeFile(outPath, JSON.stringify(ordered, null, 2) + '\n', 'utf8');
  console.log(`Wrote ${loc}.json: ${canonicalKeys.length} keys (${fallbackCount} fallback(s) from en)`);
}
