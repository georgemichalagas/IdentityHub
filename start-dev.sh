#!/bin/bash

# Load environment variables
if [ -f ".env.development" ]; then
    export $(cat .env.development | grep -v '^#' | xargs)
    echo "✅ Loaded development environment variables"
fi

# Start PostgreSQL with Docker Compose
echo "Starting PostgreSQL..."
docker-compose up -d postgres

# Wait for PostgreSQL to be ready
echo "Waiting for PostgreSQL to be ready..."
until docker exec identityhub-postgres pg_isready -U postgres; do
  echo "PostgreSQL is unavailable - sleeping"
  sleep 1
done

echo "PostgreSQL is ready!"

# Start the application
echo "Starting IdentityHub API..."
cd IdentityHub.Api
export ASPNETCORE_URLS="http://localhost:${API_PORT:-5040}"
echo "🚀 API will be available at: $ASPNETCORE_URLS"
dotnet run
