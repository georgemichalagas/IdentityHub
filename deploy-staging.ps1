# Deploy to Staging Environment
Write-Host "🚀 Deploying IdentityHub to Staging Environment..." -ForegroundColor Green

# Load environment variables
if (Test-Path ".env.staging") {
    Get-Content ".env.staging" | ForEach-Object {
        if ($_ -match "^([^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
    Write-Host "✅ Loaded staging environment variables" -ForegroundColor Green
} else {
    Write-Host "❌ .env.staging file not found. Creating from template..." -ForegroundColor Red
    Copy-Item ".env.staging" ".env.staging.local"
    Write-Host "⚠️  Please update .env.staging.local with your staging values and rename to .env.staging" -ForegroundColor Yellow
    exit 1
}

# Build and deploy with Docker Compose
Write-Host "🏗️  Building and starting services..." -ForegroundColor Yellow
docker-compose -f docker-compose.staging.yml down
docker-compose -f docker-compose.staging.yml build --no-cache
docker-compose -f docker-compose.staging.yml up -d

# Wait for services to be ready
Write-Host "⏳ Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep 30

# Get API port from environment variable
$apiPort = [Environment]::GetEnvironmentVariable("API_PORT")
if (-not $apiPort) { $apiPort = "7080" }

# Check if API is responding
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$apiPort/health" -TimeoutSec 10
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Staging deployment successful!" -ForegroundColor Green
        Write-Host "🌐 API available at: http://localhost:$apiPort" -ForegroundColor Cyan
        Write-Host "📊 Swagger UI: http://localhost:$apiPort" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Deployment failed - API is not responding" -ForegroundColor Red
    Write-Host "📋 Checking logs..." -ForegroundColor Yellow
    docker-compose -f docker-compose.staging.yml logs identityhub-api
    exit 1
}

Write-Host "🎉 Staging environment is ready!" -ForegroundColor Green
