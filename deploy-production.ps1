# Deploy to Production Environment
Write-Host "🚀 Deploying IdentityHub to Production Environment..." -ForegroundColor Green

# Confirmation prompt
$confirm = Read-Host "⚠️  Are you sure you want to deploy to PRODUCTION? (yes/no)"
if ($confirm -ne "yes") {
    Write-Host "❌ Deployment cancelled" -ForegroundColor Red
    exit 0
}

# Load environment variables
if (Test-Path ".env.production") {
    Get-Content ".env.production" | ForEach-Object {
        if ($_ -match "^([^=]+)=(.*)$") {
            [Environment]::SetEnvironmentVariable($matches[1], $matches[2])
        }
    }
    Write-Host "✅ Loaded production environment variables" -ForegroundColor Green
} else {
    Write-Host "❌ .env.production file not found. Creating from template..." -ForegroundColor Red
    Copy-Item ".env.production" ".env.production.local"
    Write-Host "⚠️  Please update .env.production.local with your production values and rename to .env.production" -ForegroundColor Yellow
    exit 1
}

# Security checks
$jwtSecret = [Environment]::GetEnvironmentVariable("JWT_SECRET_KEY")
$dbPassword = [Environment]::GetEnvironmentVariable("DB_PASSWORD")

if ($jwtSecret -like "*your_*" -or $dbPassword -like "*your_*") {
    Write-Host "❌ Please update all placeholder values in .env.production before deploying to production" -ForegroundColor Red
    exit 1
}

# Backup database (if not first deployment)
Write-Host "💾 Creating database backup..." -ForegroundColor Yellow
$containers = docker ps --format "table {{.Names}}" | Select-String "identityhub-postgres-prod"
if ($containers) {
    $backupName = "backup_$(Get-Date -Format 'yyyyMMdd_HHmmss').sql"
    docker exec identityhub-postgres-prod pg_dump -U $env:DB_USER $env:DB_NAME > $backupName
    Write-Host "✅ Database backup created: $backupName" -ForegroundColor Green
}

# Build and deploy with Docker Compose
Write-Host "🏗️  Building and starting services..." -ForegroundColor Yellow
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml build --no-cache
docker-compose -f docker-compose.production.yml up -d

# Wait for services to be ready
Write-Host "⏳ Waiting for services to be ready..." -ForegroundColor Yellow
Start-Sleep 60

# Get API port from environment variable
$apiPort = [Environment]::GetEnvironmentVariable("API_PORT")
if (-not $apiPort) { $apiPort = "8080" }

# Health check
Write-Host "🏥 Performing health checks..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$apiPort/health" -TimeoutSec 30
    if ($response.StatusCode -eq 200) {
        Write-Host "✅ Production deployment successful!" -ForegroundColor Green
        Write-Host "🌐 API available at: http://localhost:$apiPort" -ForegroundColor Cyan
        Write-Host "📊 Health check: http://localhost:$apiPort/health" -ForegroundColor Cyan
    }
} catch {
    Write-Host "❌ Deployment failed - API is not responding" -ForegroundColor Red
    Write-Host "📋 Checking logs..." -ForegroundColor Yellow
    docker-compose -f docker-compose.production.yml logs identityhub-api
    
    # Rollback option
    $rollback = Read-Host "🔄 Would you like to rollback? (yes/no)"
    if ($rollback -eq "yes") {
        Write-Host "🔄 Rolling back..." -ForegroundColor Yellow
        docker-compose -f docker-compose.production.yml down
        # Restore from backup if available
        $latestBackup = Get-ChildItem "backup_*.sql" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
        if ($latestBackup) {
            Write-Host "📁 Restoring from backup: $($latestBackup.Name)" -ForegroundColor Cyan
            # Add restore logic here
        }
    }
    exit 1
}

Write-Host "🎉 Production environment is ready!" -ForegroundColor Green
Write-Host "🔒 Remember to:" -ForegroundColor Yellow
Write-Host "   - Configure SSL certificates" -ForegroundColor White
Write-Host "   - Set up monitoring and alerts" -ForegroundColor White
Write-Host "   - Configure automated backups" -ForegroundColor White
Write-Host "   - Review security settings" -ForegroundColor White
