#!/usr/bin/env node
/**
 * Enforces comprehensive translation QA across English, Arabic, and Urdu locales:
 * 1. 100% key parity across en, ar, and ur.
 * 2. Exact {{PLACEHOLDER}} token set parity per key across en, ar, and ur.
 * 3. Zero unconverted Angular $localize / XLF artifacts ({$VAR}, :VAR:, START_TAG_*, CLOSE_TAG_*).
 * 4. Zero empty values, U+FFFD replacement characters, or stale "Education Portal" brand leaks.
 * 5. Verified Arabic/Urdu script presence in ar.json and ur.json (no silently untranslated English entries).
 */
import { readFile } from 'node:fs/promises';
import path from 'node:path';

const localesDir = path.resolve(
  path.dirname(new URL(import.meta.url).pathname.replace(/^\/([A-Za-z]:)/, '$1')),
  '../src/locales'
);

const en = JSON.parse(await readFile(path.join(localesDir, 'en.json'), 'utf8'));
const ar = JSON.parse(await readFile(path.join(localesDir, 'ar.json'), 'utf8'));
const ur = JSON.parse(await readFile(path.join(localesDir, 'ur.json'), 'utf8'));

const enKeys = Object.keys(en).sort();
const errors = [];
const placeholderRe = /\{\{([A-Za-z0-9_]+)\}\}/g;
const arabicScriptRe = /[\u0600-\u06FF\u0750-\u077F\u08A0-\u08FF\uFB50-\uFDFF\uFE70-\uFEFF]/;

const extractPlaceholders = (text) => {
  const matches = [];
  for (const m of text.matchAll(placeholderRe)) {
    matches.push(m[1]);
  }
  return matches.sort();
};

let placeholderKeyCount = 0;

for (const [name, dict] of [['en', en], ['ar', ar], ['ur', ur]]) {
  const keys = Object.keys(dict).sort();
  const missing = enKeys.filter((k) => !(k in dict));
  const extra = keys.filter((k) => !(k in en));
  if (missing.length) errors.push(`${name}.json missing ${missing.length} key(s): ${missing.slice(0, 5).join(', ')}`);
  if (extra.length) errors.push(`${name}.json has ${extra.length} extra key(s): ${extra.slice(0, 5).join(', ')}`);

  for (const [k, v] of Object.entries(dict)) {
    if (typeof v !== 'string' || !v.trim()) {
      errors.push(`${name}.json:${k} is empty`);
      continue;
    }
    if (v.includes('\uFFFD')) errors.push(`${name}.json:${k} contains corrupt Unicode replacement char (U+FFFD)`);
    if (/\{\$[A-Za-z0-9_]+\}/.test(v)) errors.push(`${name}.json:${k} contains unconverted Angular {$VAR} placeholder: ${v}`);
    if (/:[A-Z][A-Z0-9_]+:/.test(v)) errors.push(`${name}.json:${k} contains unconverted Angular $localize :VAR: suffix: ${v}`);
    if (/\{\{(?:START_TAG_|CLOSE_TAG_)[A-Za-z0-9_]+\}\}/.test(v)) errors.push(`${name}.json:${k} contains unconverted Angular XLF HTML tag placeholder: ${v}`);
    if (/Education Portal/i.test(v)) errors.push(`${name}.json:${k} contains stale Education Portal branding`);
    if ((name === 'ar' || name === 'ur') && !arabicScriptRe.test(v)) {
      errors.push(`${name}.json:${k} contains no Arabic/Urdu script characters (likely untranslated): ${v}`);
    }
  }
}

for (const k of enKeys) {
  const enPh = extractPlaceholders(String(en[k] ?? ''));
  const arPh = extractPlaceholders(String(ar[k] ?? ''));
  const urPh = extractPlaceholders(String(ur[k] ?? ''));
  if (enPh.length > 0) placeholderKeyCount++;
  if (JSON.stringify(enPh) !== JSON.stringify(arPh)) {
    errors.push(`Placeholder mismatch on "${k}": en=[${enPh.join(', ')}] vs ar=[${arPh.join(', ')}]`);
  }
  if (JSON.stringify(enPh) !== JSON.stringify(urPh)) {
    errors.push(`Placeholder mismatch on "${k}": en=[${enPh.join(', ')}] vs ur=[${urPh.join(', ')}]`);
  }
}

if (errors.length) {
  console.error(`Locale verification failed (${errors.length} error(s)):`);
  for (const err of errors) console.error(`  - ${err}`);
  process.exit(1);
}

console.log(
  `Locale verification passed: ${enKeys.length} keys (${placeholderKeyCount} interpolated) with 100% key, placeholder, and script parity across en, ar, and ur.`
);
