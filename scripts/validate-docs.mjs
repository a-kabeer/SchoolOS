#!/usr/bin/env node
import { readdir, readFile, stat } from "node:fs/promises";
import path from "node:path";

const root = process.cwd();
const errors = [];
const warnings = [];
const requiredMetadata = [
  "Document ID", "Status", "Decision maturity", "Owner", "Last reviewed", "Source baseline"
];

async function walk(dir) {
  const out = [];
  let entries;
  try { entries = await readdir(dir, { withFileTypes: true }); }
  catch { return out; }
  for (const entry of entries) {
    const full = path.join(dir, entry.name);
    if (entry.isDirectory()) out.push(...await walk(full));
    else out.push(full);
  }
  return out;
}

function parseCsv(source) {
  const rows = [];
  let row = [], value = "", quoted = false;
  for (let i = 0; i < source.length; i++) {
    const ch = source[i];
    if (quoted) {
      if (ch === '"' && source[i + 1] === '"') { value += '"'; i++; }
      else if (ch === '"') quoted = false;
      else value += ch;
    } else if (ch === '"') quoted = true;
    else if (ch === ",") { row.push(value); value = ""; }
    else if (ch === "\n") { row.push(value.replace(/\r$/, "")); rows.push(row); row = []; value = ""; }
    else value += ch;
  }
  if (value.length || row.length) { row.push(value.replace(/\r$/, "")); rows.push(row); }
  if (quoted) throw new Error("unclosed quoted field");
  return rows.filter(r => r.some(v => v !== ""));
}

function slugify(text) {
  return text.toLowerCase().trim()
    .replace(/<[^>]+>/g, "")
    .replace(/\[([^\]]+)\]\([^)]+\)/g, "$1")
    .replace(/[`*_~]/g, "")
    .replace(/[^\p{L}\p{N}\s-]/gu, "")
    .replace(/\s+/g, "-")
    .replace(/-+/g, "-");
}

function headingIds(text) {
  const result = new Set();
  const counts = new Map();
  for (const match of text.matchAll(/^#{1,6}\s+(.+?)\s*#*\s*$/gm)) {
    const base = slugify(match[1]);
    const count = counts.get(base) || 0;
    counts.set(base, count + 1);
    result.add(count ? `${base}-${count}` : base);
  }
  return result;
}

const docsDir = path.join(root, "docs");
const docs = (await walk(docsDir)).filter(p => p.endsWith(".md"));
const navigationFiles = ["README.md", "AGENTS.md", ".agents/skills/README.md", "registers/README.md"].map(p => path.join(root, p));
const textByPath = new Map();
for (const file of [...docs, ...navigationFiles]) textByPath.set(file, await readFile(file, "utf8"));
const documentIds = new Map();

for (const [file, source] of textByPath) {
  const rel = path.relative(root, file).split(path.sep).join("/");
  const exempt = rel === "README.md" || rel === "AGENTS.md" || rel === ".agents/skills/README.md" || rel === "registers/README.md" || rel === "docs/README.md" || rel === "docs/13_ADR_INDEX.md" || rel.startsWith("docs/templates/");
  if (!exempt) {
    const opening = source.split(/\r?\n/).slice(0, 24).join("\n");
    for (const field of requiredMetadata) {
      const pattern = new RegExp(`^\\s*- \\*\\*${field}:\\*\\*\\s*\\S`, "m");
      if (!pattern.test(opening)) errors.push(`${rel}: missing metadata field "${field}"`);
    }
    const idMatch = opening.match(/^\s*-\s+\*\*Document ID:\*\*\s*(.+?)\s*$/m);
    if (!idMatch) errors.push(`${rel}: missing canonical Document ID`);
    else {
      const documentId = idMatch[1].trim();
      if (documentIds.has(documentId)) errors.push(`duplicate canonical Document ID "${documentId}": ${documentIds.get(documentId)} and ${rel}`);
      else documentIds.set(documentId, rel);
    }
  }
  const fenceCount = (source.match(/^\s*```/gm) || []).length;
  if (fenceCount % 2 !== 0) errors.push(`${rel}: unbalanced fenced code block`);

  const linkRe = /!?\[[^\]]*\]\(([^)]+)\)/g;
  let match;
  while ((match = linkRe.exec(source)) !== null) {
    let destination = match[1].trim();
    destination = destination.replace(/\s+["'][^"']*["']\s*$/, "");
    if (!destination || /^(https?:|mailto:|tel:|data:|javascript:)/i.test(destination) || destination.startsWith("//")) continue;
    const hashIndex = destination.indexOf("#");
    const queryIndex = destination.indexOf("?");
    const pathEnd = [hashIndex, queryIndex].filter(n => n >= 0).reduce((a,b)=>Math.min(a,b), destination.length);
    const rawPath = destination.slice(0, pathEnd);
    const fragment = hashIndex >= 0 ? destination.slice(hashIndex + 1).split("?")[0] : "";
    let target;
    try {
      const decoded = decodeURIComponent(rawPath);
      target = decoded.startsWith("/")
        ? path.resolve(root, decoded.slice(1))
        : path.resolve(path.dirname(file), decoded || path.basename(file));
    } catch {
      errors.push(`${rel}: invalid encoded relative link "${destination}"`);
      continue;
    }
    let targetIsFile = false;
    try { targetIsFile = (await stat(target)).isFile(); } catch {}
    if (!targetIsFile) {
      let hasReadme = false;
      try { hasReadme = (await stat(path.join(target, "README.md"))).isFile(); } catch {}
      if (!hasReadme) {
        errors.push(`${rel}: broken relative link "${destination}"`);
        continue;
      }
    }
    if (fragment && targetIsFile) {
      const targetText = textByPath.get(target) ?? await readFile(target, "utf8").catch(() => "");
      const ids = headingIds(targetText);
      let decodedFragment = fragment;
      try { decodedFragment = decodeURIComponent(fragment); } catch {}
      if (ids.size && !ids.has(decodedFragment.toLowerCase())) {
        warnings.push(`${rel}: heading anchor not verified "${destination}"`);
      }
    }
  }
}


// Ensure the categorized catalogue links every numbered specification.
const docIndex = textByPath.get(path.join(root, "docs", "README.md")) || "";
for (const file of docs.filter(p => /^\d{2}_[A-Z0-9_]+\.md$/.test(path.basename(p)))) {
  const name = path.basename(file);
  if (!docIndex.includes("](" + name + ")")) errors.push("docs/README.md does not index " + name);
}
if (!(textByPath.get(path.join(root, "README.md")) || "").includes("docs/README.md")) errors.push("root README.md does not link to docs/README.md");
const registerFiles = (await walk(path.join(root, "registers")))
  .filter(p => /^task-register-.*\.csv$/.test(path.basename(p)));
const expectedHeader = ["Task ID","ID Type","Workstream","Work Package","Task","Source Document","Source ID","Priority","Status","Owner"];
const allTaskIds = new Set();
const registerRowCounts = new Map();
let taskCount = 0;
for (const file of registerFiles) {
  const rel = path.relative(root, file).split(path.sep).join("/");
  let rows;
  try { rows = parseCsv(await readFile(file, "utf8")); }
  catch (error) { errors.push(`${rel}: CSV parse error: ${error.message}`); continue; }
  const header = rows[0] || [];
  if (header.join("|") !== expectedHeader.join("|")) errors.push(`${rel}: unexpected CSV header`);
  for (const row of rows.slice(1)) {
    if (row.length !== header.length) errors.push(`${rel}: row has ${row.length} columns; expected ${header.length}`);
    const id = row[0] || "";
    if (!id) { errors.push(`${rel}: task row has no ID`); continue; }
    if (allTaskIds.has(id)) errors.push(`duplicate task ID across register partitions: ${id}`);
    allTaskIds.add(id);
    taskCount++;
    if (!row[8]) errors.push(`${rel}: ${id} has no status`);
    if (!row[5]) errors.push(`${rel}: ${id} has no source document`);
  }
  registerRowCounts.set(rel, rows.length - 1);
}
if (registerFiles.length !== 8) errors.push(`expected 8 task-register partitions; found ${registerFiles.length}`);

let requirementCount = 0;
const requirementIds = new Set();
try {
  const reqRows = parseCsv(await readFile(path.join(root, "registers", "requirement-traceability.csv"), "utf8"));
  const reqHeader = reqRows[0] || [];
  const requiredColumns = ["Requirement ID","Implementation Task IDs","Task Mapping Status","Acceptance Test References","Evidence Status"];
  for (const col of requiredColumns) if (!reqHeader.includes(col)) errors.push(`requirement-traceability.csv missing column "${col}"`);
  const index = Object.fromEntries(reqHeader.map((h,i)=>[h,i]));
  const testSource = await readFile(path.join(root,"docs","47_WORKFLOW_ACCEPTANCE_TEST_CATALOGUE.md"),"utf8");
  const knownTests = new Set([...testSource.matchAll(/^### ([A-Z]{2,5}-\d{3}) —/gm)].map(m=>m[1]));
  for (const row of reqRows.slice(1)) {
    requirementCount++;
    const id = row[index["Requirement ID"]] || "";
    if (!id) { errors.push("requirement row missing Requirement ID"); continue; }
    if (requirementIds.has(id)) errors.push(`duplicate requirement ID: ${id}`);
    requirementIds.add(id);
    const taskText = row[index["Implementation Task IDs"]] || "";
    if (!taskText.trim()) errors.push(`${id}: use task IDs or explicit Unassigned marker`);
    if (taskText.trim() !== "Unassigned") {
      for (const taskId of taskText.split(";").map(s=>s.trim()).filter(Boolean)) {
        if (!allTaskIds.has(taskId)) errors.push(`${id}: mapped task ID does not exist: ${taskId}`);
      }
    }
    const refs = row[index["Acceptance Test References"]] || "";
    const testIds = refs.match(/[A-Z]{2,5}-\d{3}/g) || [];
    if (!testIds.length) errors.push(`${id}: no acceptance test ID`);
    for (const testId of testIds) if (!knownTests.has(testId)) errors.push(`${id}: acceptance-test ID missing in document 47: ${testId}`);
    if (!row[index["Evidence Status"]]) errors.push(`${id}: evidence status is empty`);
  }
} catch (error) {
  errors.push(`requirement traceability validation failed: ${error.message}`);
}


// Reconcile counts.json with the actual CSV source rows.
try {
  const counts = JSON.parse(await readFile(path.join(root, "registers", "task-register-counts.json"), "utf8"));
  if (!Array.isArray(counts.task_registers)) errors.push("task-register-counts.json must contain task_registers");
  if ((counts.task_registers || []).length !== registerFiles.length) errors.push("task-register-counts.json partition count does not match CSV partitions");
  for (const item of counts.task_registers || []) {
    const actual = registerRowCounts.get(item.path);
    if (actual === undefined) errors.push("task-register-counts.json references missing partition: " + item.path);
    else if (actual !== item.data_rows) errors.push(item.path + ": manifest says " + item.data_rows + " rows but CSV has " + actual);
  }
  for (const rel of registerRowCounts.keys()) if (!(counts.task_registers || []).some(item => item.path === rel)) errors.push("task-register-counts.json omits partition: " + rel);
  if (counts.total_task_register_rows !== taskCount) errors.push("task-register-counts.json total task rows do not match CSVs");
  if (counts.requirement_traceability_rows !== requirementCount) errors.push("task-register-counts.json requirement rows do not match CSV");
  if (taskCount !== 309) errors.push("expected 309 task rows in the current documentation baseline");
  if (requirementCount !== 37) errors.push("expected 37 requirement rows in the current documentation baseline");
} catch (error) { errors.push("task-register-counts.json validation failed: " + error.message); }

// DEC-001 through DEC-023 must occupy one consistent six-column primary table.
try {
  const source = await readFile(path.join(root, "docs", "60_ARCHITECTURE_AND_SECURITY_DECISION_CLOSURE_REGISTER.md"), "utf8");
  const start = source.indexOf("## Decision closure register");
  const end = source.indexOf("## SaaS tenant implementation baseline proposal", start);
  if (start < 0 || end < 0) errors.push("DOC-60 primary decision table boundaries are missing");
  else {
    const lines = source.slice(start, end).split(/\r?\n/).filter(line => /^\s*\|/.test(line));
    if (lines.length < 25) errors.push("DOC-60 primary decision table is incomplete");
    if (lines.length >= 2 && (lines[0].trim().replace(/^\|/, "").replace(/\|$/, "").split("|").length !== 6 || lines[1].trim().replace(/^\|/, "").replace(/\|$/, "").split("|").length !== 6)) errors.push("DOC-60 primary decision table header must have six columns");
    const rows = lines.filter(line => /^\s*\|\s*DEC-\d{3}\s*\|/.test(line));
    const ids = rows.map(line => line.trim().replace(/^\|/, "").split("|")[0].trim());
    if (rows.length !== 23) errors.push("DOC-60 primary decision table must contain exactly 23 rows; found " + rows.length);
    for (const line of rows) if (line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").length !== 6) errors.push("DOC-60 decision row must have six columns: " + line.slice(0, 80));
    for (let i = 1; i <= 23; i++) { const id = "DEC-" + String(i).padStart(3, "0"); if (ids.filter(value => value === id).length !== 1) errors.push("DOC-60 must contain " + id + " exactly once"); }
  }
} catch (error) { errors.push("DOC-60 table validation failed: " + error.message); }
const adrIndex = await readFile(path.join(root,"docs","13_ADR_INDEX.md"),"utf8");
for (const file of (await walk(path.join(root,"docs","decisions"))).filter(p=>/^ADR-\d{3}-.*\.md$/.test(path.basename(p)))) {
  const name = path.basename(file);
  if (!adrIndex.includes(name)) errors.push(`ADR index does not list ${name}`);
}

if (errors.length) {
  console.error(`Documentation validation failed (${errors.length} error(s)):`);
  for (const error of errors) console.error(`- ${error}`);
  if (warnings.length) console.error(`Non-blocking anchor warnings: ${warnings.length}`);
  process.exitCode = 1;
} else {
  console.log(`Documentation validation passed: ${docs.length} Markdown files; ${registerFiles.length} task-register partitions; ${taskCount} unique tasks; ${requirementCount} requirements; ${documentIds.size} unique canonical Document IDs.`);
  if (warnings.length) console.log(`Non-blocking heading-anchor warnings: ${warnings.length}`);
}
