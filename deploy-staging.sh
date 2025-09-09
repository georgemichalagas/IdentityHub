#!/bin/bash

# Deploy to Staging Environment
echo "🚀 Deploying IdentityHub to Staging Environment..."

# Load environment variables
if [ -f .env.staging ]; then
    export $(cat .env.staging | xargs)
    echo "✅ Loaded staging environment variables"
else
    echo "❌ .env.staging file not found. Creating from template..."
    cp .env.staging .env.staging.local
    echo "⚠️  Please update .env.staging.local with your staging values and rename to .env.staging"
    exit 1
fi

# Build and deploy with Docker Compose
echo "🏗️  Building and starting services..."
docker-compose -f docker-compose.staging.yml down
docker-compose -f docker-compose.staging.yml build --no-cache
docker-compose -f docker-compose.staging.yml up -d

# Wait for services to be ready
echo "⏳ Waiting for services to be ready..."
sleep 30

# Get API port from environment variable
API_PORT=${API_PORT:-7080}

# Check if API is responding
if curl -f http://localhost:$API_PORT/health > /dev/null 2>&1; then
    echo "✅ Staging deployment successful!"
    echo "🌐 API available at: http://localhost:$API_PORT"
    echo "📊 Swagger UI: http://localhost:$API_PORT"
else
    echo "❌ Deployment failed - API is not responding"
    echo "📋 Checking logs..."
    docker-compose -f docker-compose.staging.yml logs identityhub-api
    exit 1
fi

echo "🎉 Staging environment is ready!"
