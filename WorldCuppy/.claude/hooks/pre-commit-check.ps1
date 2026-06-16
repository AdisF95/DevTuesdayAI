$raw = [Console]::In.ReadToEnd()
if (-not $raw) { exit 0 }

$json = $raw | ConvertFrom-Json -ErrorAction SilentlyContinue
if (-not $json -or $json.command -notmatch 'git\s+commit') { exit 0 }

Write-Host "Pre-commit: building..." -ForegroundColor Cyan
dotnet build WorldCuppy/WorldCuppy.csproj --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "Build FAILED — commit blocked." -ForegroundColor Red
    exit 2
}

Write-Host "Pre-commit: running tests..." -ForegroundColor Cyan
dotnet test WorldCuppy.slnx --no-build --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    Write-Host "Tests FAILED — commit blocked." -ForegroundColor Red
    exit 2
}

Write-Host "Green. Commit allowed." -ForegroundColor Green
exit 0
