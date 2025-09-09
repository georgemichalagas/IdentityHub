#!/bin/bash

# Deploy to Production Environment
echo "🚀 Deploying IdentityHub to Production Environment..."

# Confirmation prompt
read -p "⚠️  Are you sure you want to deploy to PRODUCTION? (yes/no): " confirm
if [ "$confirm" != "yes" ]; then
    echo "❌ Deployment cancelled"
    exit 0
fi

# Load environment variables
if [ -f .env.production ]; then
    export $(cat .env.production | xargs)
    echo "✅ Loaded production environment variables"
else
    echo "❌ .env.production file not found. Creating from template..."
    cp .env.production .env.production.local
    echo "⚠️  Please update .env.production.local with your production values and rename to .env.production"
    exit 1
fi

# Security checks
if [[ "$JWT_SECRET_KEY" == *"your_"* ]] || [[ "$DB_PASSWORD" == *"your_"* ]]; then
    echo "❌ Please update all placeholder values in .env.production before deploying to production"
    exit 1
fi

# Backup database (if not first deployment)
echo "💾 Creating database backup..."
if docker ps | grep -q identityhub-postgres-prod; then
    docker exec identityhub-postgres-prod pg_dump -U $DB_USER $DB_NAME > "backup_$(date +%Y%m%d_%H%M%S).sql"
    echo "✅ Database backup created"
fi

# Build and deploy with Docker Compose
echo "🏗️  Building and starting services..."
docker-compose -f docker-compose.production.yml down
docker-compose -f docker-compose.production.yml build --no-cache
docker-compose -f docker-compose.production.yml up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 60

# Get API port from environment variable
API_PORT=${API_PORT:-8080}

# Health check
echo "🏥 Performing health checks..."
if curl -f http://localhost:$API_PORT/health > /dev/null 2>&1; then
    echo "✅ Production deployment successful!"
    echo "🌐 API available at: http://localhost:$API_PORT"
    echo "📊 Health check: http://localhost:$API_PORT/health"
else
    echo "❌ Deployment failed - API is not responding"
    echo "📋 Checking logs..."
    docker-compose -f docker-compose.production.yml logs identityhub-api
    
    # Rollback option
    read -p "🔄 Would you like to rollback? (yes/no): " rollback
    if [ "$rollback" == "yes" ]; then
        echo "🔄 Rolling back..."
        docker-compose -f docker-compose.production.yml down
        # Restore from backup if available
        latest_backup=$(ls -t backup_*.sql 2>/dev/null | head -n1)
        if [ -n "$latest_backup" ]; then
            echo "📁 Restoring from backup: $latest_backup"
            # Add restore logic here
        fi
    fi
    exit 1
fi

echo "🎉 Production environment is ready!"
echo "🔒 Remember to:"
echo "   - Configure SSL certificates"
echo "   - Set up monitoring and alerts"
echo "   - Configure automated backups"
echo "   - Review security settings"
