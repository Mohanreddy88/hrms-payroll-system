#!/bin/bash
set -e

echo "Starting HRMS API..."
echo "Waiting for database to be ready..."
sleep 5

echo "Running database migrations..."
dotnet ef database update --no-build || echo "Migration failed or already applied"

echo "Starting application..."
exec dotnet HrmsApi.dll
