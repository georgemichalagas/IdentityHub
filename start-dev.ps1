# Load environment variables
if (Test-Path ".env.development") {
    Get-Content ".env.development" | ForEach-Object {
        if ($_ -match "^([^#][^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
    Write-Host "✅ Loaded development environment variables" -ForegroundColor Green
}

# Start PostgreSQL with Docker Compose
Write-Host "Starting PostgreSQL..." -ForegroundColor Green
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
Write-Host "Waiting for PostgreSQL to be ready..." -ForegroundColor Yellow
do {
    $result = docker exec identityhub-postgres pg_isready -U postgres 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "PostgreSQL is unavailable - sleeping" -ForegroundColor Yellow
        Start-Sleep 1
    }
} while ($LASTEXITCODE -ne 0)

Write-Host "PostgreSQL is ready!" -ForegroundColor Green

# Start the application
Write-Host "Starting IdentityHub API..." -ForegroundColor Green
$apiPort = [Environment]::GetEnvironmentVariable("API_PORT")
if (-not $apiPort) { $apiPort = "5040" }
$env:ASPNETCORE_URLS = "http://localhost:$apiPort"
Write-Host "🚀 API will be available at: $env:ASPNETCORE_URLS" -ForegroundColor Cyan
Set-Location IdentityHub.Api
dotnet run
