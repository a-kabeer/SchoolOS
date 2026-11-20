#!/usr/bin/env python3
"""SchoolOS automated security and engineering compliance guard (adapted from Education-Portal for .NET 10 + React).

Runs in CI (.github/workflows/ci.yml) or locally:
  python3 scripts/compliance-guard.py
"""
from __future__ import annotations

import re
import sys
from dataclasses import dataclass
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
EXCEPTION = re.compile(r"(compliance-exception:\s*SEC|engineering-exception:\s*ENG)-[A-Z]+-\d+\s+\S")
SKIP_DIRS = {".git", "node_modules", "artifacts", "bin", "obj", "dist", "TestResults", ".agents"}
SKIP_PREFIXES = ("docs/", "scripts/compliance-guard.py")
TEXT_EXT = {".cs", ".ts", ".tsx", ".html", ".js", ".mjs", ".json", ".csproj", ".props", ".targets", ".yml", ".yaml", ".env"}


@dataclass(frozen=True)
class Check:
    id: str
    rule: str
    message: str
    files: re.Pattern
    pattern: re.Pattern


def c(id_: str, rule: str, message: str, files: str, pattern: str, flags: int = 0) -> Check:
    return Check(id_, rule, message, re.compile(files), re.compile(pattern, flags))


SRC_CS = r"^src/.*\.cs$"
ALL_CS = r"\.cs$"
REACT = r"^frontend/schoolos-web/src/.*\.(ts|tsx|html)$"

CHECKS = [
    c("cors-any-origin", "SEC-WEB-02", "AllowAnyOrigin is forbidden; restrict CORS origins explicitly", ALL_CS, r"\bAllowAnyOrigin\s*\("),
    c("raw-sql", "SEC-INPUT-02", "raw SQL API; use LINQ or parameterized FromSql/ExecuteSql", SRC_CS, r"\b(FromSqlRaw|SqlQueryRaw)\s*[<(]"),
    c("ignore-query-filters", "SEC-AUTHZ-03", "IgnoreQueryFilters bypasses tenant isolation; document explicit exception if justified", SRC_CS, r"\bIgnoreQueryFilters\s*\("),
    c("weak-crypto", "SEC-CRYPTO-01", "weak or forbidden cryptography (MD5/SHA1/DES/ECB)", ALL_CS, r"\b(MD5|SHA1|DES|TripleDES|RC2)\s*\.\s*(Create|HashData)\b|CipherMode\.ECB"),
    c("disable-tls-validation", "SEC-CRYPTO-03", "TLS certificate validation disabled", r"\.(cs|json)$", r"DangerousAcceptAnyServerCertificateValidator|ServerCertificateCustomValidationCallback\s*=\s*\(?.*=>\s*true|TrustServerCertificate\s*=\s*true", re.I),
    c("unsafe-deserialization", "SEC-INPUT-06", "unsafe polymorphic deserialization", ALL_CS, r"TypeNameHandling\s*\.\s*(All|Auto|Objects|Arrays)|\bBinaryFormatter\b"),
    c("react-xss-sink", "SEC-INPUT-03", "raw HTML or dynamic code execution in React frontend", REACT, r"\bdangerouslySetInnerHTML\b|\.(innerHTML|outerHTML)\s*=|insertAdjacentHTML\s*\(|\beval\s*\(|\bnew\s+Function\s*\("),
    c("react-token-localstorage", "SEC-AUTHN-04", "never store auth tokens in localStorage/sessionStorage (ADR-023)", REACT, r"\b(localStorage|sessionStorage)\s*\.\s*setItem\s*\([^)]*(token|jwt|auth)", re.I),
    c("package-version-in-csproj", "SEC-DEP-01", "package versions belong centrally in Directory.Packages.props", r"\.csproj$", r"<PackageReference\b[^>]*\bVersion\s*="),
    c("nuget-audit-suppressed", "SEC-DEP-02", "NuGet vulnerability warnings must not be suppressed", r"\.(csproj|props|targets)$", r"NU190[1-4]"),
    c("private-key", "SEC-SECRET-01", "private key committed to source", r".*", r"-----BEGIN (RSA |EC |DSA |OPENSSH |ENCRYPTED )?PRIVATE KEY-----"),
    c("card-data-field", "SEC-PCI-02", "raw payment card fields must never be handled directly", r"\.(cs|ts|tsx)$", r"(?i)\b(credit_?card_?number|primary_?account_?number|cvv2?|cvc2?|track_?[12]_?data|mag_?stripe)\b"),
    c("sync-over-async", "ENG-EF-06", "avoid sync-over-async blocking (.Result/.Wait/.GetResult)", SRC_CS, r"\.(Result|Wait\(\s*\))(?![\w(])|\.GetAwaiter\(\)\s*\.\s*GetResult\(\)"),
]


def iter_files() -> list[Path]:
    out: list[Path] = []
    for p in ROOT.rglob("*"):
        if not p.is_file():
            continue
        if any(part in SKIP_DIRS for part in p.parts):
            continue
        if p.suffix.lower() not in TEXT_EXT:
            continue
        rel = p.relative_to(ROOT).as_posix()
        if any(rel.startswith(pref) for pref in SKIP_PREFIXES):
            continue
        out.append(p)
    return sorted(out)


def main() -> int:
    findings: list[str] = []
    files = iter_files()
    for path in files:
        rel = path.relative_to(ROOT).as_posix()
        matching = [chk for chk in CHECKS if chk.files.search(rel)]
        if not matching:
            continue
        try:
            lines = path.read_text(encoding="utf-8", errors="ignore").splitlines()
        except OSError:
            continue
        for idx, line in enumerate(lines, start=1):
            stripped = line.lstrip()
            if stripped.startswith(("//", "#", "*")):
                continue
            prev = lines[idx - 2] if idx >= 2 else ""
            if EXCEPTION.search(line) or EXCEPTION.search(prev):
                continue
            for chk in matching:
                if chk.pattern.search(line):
                    findings.append(f"{rel}:{idx}: [{chk.rule}] {chk.id}: {chk.message}")

    if findings:
        print(f"SchoolOS compliance guard failed ({len(findings)} finding(s)):")
        for f in findings:
            print(f"  - {f}")
        return 1

    print(f"SchoolOS compliance guard passed ({len(files)} files scanned, {len(CHECKS)} rules active).")
    return 0


if __name__ == "__main__":
    sys.exit(main())
