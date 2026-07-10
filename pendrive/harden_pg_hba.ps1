# Harden pg_hba.conf: force every active rule to use scram-sha-256 password auth.
#
# Replaces the previous fragile inline rewrite that only matched a TAB before
# "trust" (`\ttrust$`). initdb generates pg_hba.conf with SPACES, so that regex
# was a no-op and PostgreSQL silently stayed in `trust` (no-password) mode.
# This version is whitespace-agnostic and also converts md5/ident/peer/password.

param(
    [Parameter(Mandatory = $true)][string]$PgData
)

$ErrorActionPreference = 'Stop'

$hba = Join-Path $PgData 'pg_hba.conf'
if (-not (Test-Path -LiteralPath $hba)) {
    Write-Error "pg_hba.conf not found at $hba"
    exit 1
}

# Weak/no-auth methods we replace with scram-sha-256 on active rule lines.
$methodPattern = '\b(trust|md5|ident|peer|password)\s*$'

$changed = 0
$lines = Get-Content -LiteralPath $hba
$out = foreach ($line in $lines) {
    if ($line -match '^\s*#' -or $line -match '^\s*$') {
        # Comment or blank line — leave untouched.
        $line
    }
    elseif ($line -match $methodPattern) {
        $changed++
        $line -replace $methodPattern, 'scram-sha-256'
    }
    else {
        $line
    }
}

# ASCII, no BOM — PostgreSQL will not parse a UTF-8 BOM in pg_hba.conf.
Set-Content -LiteralPath $hba -Value $out -Encoding ascii

if ($changed -eq 0) {
    Write-Warning "pg_hba.conf: no weak-auth rule lines found to convert (already scram-sha-256?)."
} else {
    Write-Output "pg_hba.conf hardened: $changed rule line(s) set to scram-sha-256."
}
exit 0
