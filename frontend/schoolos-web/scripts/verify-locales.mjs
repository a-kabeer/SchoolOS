#!/usr/bin/env node
/**
 * Enforces 100% translation key parity across English, Arabic, and Urdu locales,
 * verifies zero unconverted Angular {$VAR} syntax, and verifies zero stale "Education Portal" brand leaks.
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

for (const [name, dict] of [['en', en], ['ar', ar], ['ur', ur]]) {
  const keys = Object.keys(dict).sort();
  const missing = enKeys.filter((k) => !(k in dict));
  const extra = keys.filter((k) => !(k in en));
  if (missing.length) errors.push(`${name}.json missing ${missing.length} key(s): ${missing.slice(0, 5).join(', ')}`);
  if (extra.length) errors.push(`${name}.json has ${extra.length} extra key(s): ${extra.slice(0, 5).join(', ')}`);

  for (const [k, v] of Object.entries(dict)) {
    if (typeof v !== 'string' || !v.trim()) errors.push(`${name}.json:${k} is empty`);
    if (/\{\$[A-Za-z0-9_]+\}/.test(v)) errors.push(`${name}.json:${k} contains unconverted Angular placeholder: ${v}`);
    if (/Education Portal/i.test(v)) errors.push(`${name}.json:${k} contains stale Education Portal branding`);
  }
}

if (errors.length) {
  console.error(`Locale verification failed (${errors.length} error(s)):`);
  for (const err of errors) console.error(`  - ${err}`);
  process.exit(1);
}

console.log(`Locale verification passed: ${enKeys.length} keys with 100% parity across en, ar, and ur.`);
