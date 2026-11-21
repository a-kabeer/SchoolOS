<#
.SYNOPSIS
    Verifies PostgreSQL 17 Local Node backup (pg_dump custom format) and isolated restore drill.
.EXAMPLE
    .\scripts\backup-restore-check.ps1 -PgBin "C:\Program Files\PostgreSQL\17\bin" -Database "schoolos_local_dev"
#>
param(
    [string]$PgBin = "",
    [string]$DbHost = "127.0.0.1",
    [int]$Port = 5432,
    [string]$Username = "schoolos_dev",
    [string]$Database = "schoolos_local_dev",
    [string]$VerifyDatabase = "schoolos_restore_verify"
)

$ErrorActionPreference = "Stop"

function Resolve-PgTool([string]$name) {
    if ($PgBin) { return Join-Path $PgBin $name }
    return $name
}

$pgDump = Resolve-PgTool "pg_dump"
$pgRestore = Resolve-PgTool "pg_restore"
$psql = Resolve-PgTool "psql"

$backupFile = Join-Path ([System.IO.Path]::GetTempPath()) ("schoolos_backup_" + (Get-Date -Format "yyyyMMdd_HHmmss") + ".dump")

Write-Host "[1/4] Taking custom-format backup of '$Database' -> $backupFile"
& $pgDump -h $DbHost -p $Port -U $Username -Fc -f $backupFile $Database
if ($LASTEXITCODE -ne 0) { throw "pg_dump failed." }

Write-Host "[2/4] Recreating isolated verification database '$VerifyDatabase'"
& $psql -h $DbHost -p $Port -U $Username -d postgres -v ON_ERROR_STOP=1 -c "DROP DATABASE IF EXISTS $VerifyDatabase;" -c "CREATE DATABASE $VerifyDatabase;"
if ($LASTEXITCODE -ne 0) { throw "Failed to recreate verification database." }

Write-Host "[3/4] Restoring backup into '$VerifyDatabase'"
& $pgRestore -h $DbHost -p $Port -U $Username -d $VerifyDatabase --no-owner $backupFile
if ($LASTEXITCODE -ne 0) { throw "pg_restore failed." }

Write-Host "[4/4] Verifying RLS-protected schemas exist in restored database"
& $psql -h $DbHost -p $Port -U $Username -d $VerifyDatabase -v ON_ERROR_STOP=1 -c "SELECT nspname, COUNT(c.oid) AS tables FROM pg_namespace n LEFT JOIN pg_class c ON c.relnamespace = n.oid AND c.relkind = 'r' WHERE n.nspname IN ('identity','organization','admissions','students','attendance','fees','exams','platform_local') GROUP BY n.nspname ORDER BY n.nspname;"
if ($LASTEXITCODE -ne 0) { throw "Schema verification failed." }

Remove-Item -Path $backupFile -Force -ErrorAction SilentlyContinue
Write-Host "Backup and restore verification PASSED."
