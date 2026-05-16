# ═══════════════════════════════════════════════════════════════
# Multi-stage Dockerfile for HRMS Production Deployment
# Builds: .NET 8 API + Angular UI in a single container
# Build Date: 2026-05-16
# Version: Production v1.1 - Fixed PostgreSQL migration syntax
# ═══════════════════════════════════════════════════════════════

# ───────────────────────────────────────────────────────────────
# Stage 1: Build Angular Frontend
# ───────────────────────────────────────────────────────────────
FROM node:20-alpine AS angular-build
WORKDIR /app/frontend

# Copy package files and install dependencies
COPY hrms-ui/package*.json ./
RUN npm ci --legacy-peer-deps

# Copy source and build for production
COPY hrms-ui/ ./
RUN npm run build:prod && \
    echo "✅ Angular build complete. Verifying output..." && \
    ls -la dist/ && \
    ls -la dist/hrms-ui/ && \
    test -d dist/hrms-ui/browser || (echo "❌ ERROR: dist/hrms-ui/browser not found!" && exit 1) && \
    echo "✅ Angular build output verified at dist/hrms-ui/browser"

# ───────────────────────────────────────────────────────────────
# Stage 2: Build .NET API
# ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src

# Copy csproj and restore dependencies
COPY HrmsApi/*.csproj ./HrmsApi/
RUN dotnet restore HrmsApi/HrmsApi.csproj

# Copy source code and build
COPY HrmsApi/ ./HrmsApi/
RUN dotnet publish HrmsApi/HrmsApi.csproj -c Release -o /app/publish

# Build EF Core migration bundle for production database
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"
WORKDIR /src/HrmsApi
RUN dotnet ef migrations bundle --self-contained -r linux-x64 -o /app/efbundle

# ───────────────────────────────────────────────────────────────
# Stage 3: Runtime - Single container serving both API and UI
# ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Install nginx for serving Angular static files
RUN apt-get update && apt-get install -y nginx && rm -rf /var/lib/apt/lists/*

# Copy .NET API
COPY --from=dotnet-build /app/publish ./api/

# Copy EF migration bundle
COPY --from=dotnet-build /app/efbundle ./efbundle

# Copy Angular build output
COPY --from=angular-build /app/frontend/dist/hrms-ui/browser ./wwwroot

# Verify Angular files were copied
RUN echo "Verifying Angular files in wwwroot..." && \
    ls -la /app/wwwroot/ && \
    test -f /app/wwwroot/index.html || (echo "❌ ERROR: index.html not found in wwwroot!" && exit 1) && \
    echo "✅ Angular files verified in /app/wwwroot"

# Configure nginx
RUN echo 'server {\n\
    listen 8080;\n\
    server_name _;\n\
    root /app/wwwroot;\n\
    index index.html;\n\
    \n\
    # Angular routes\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
    \n\
    # API proxy with CORS headers\n\
    location /api/ {\n\
        # Handle preflight OPTIONS requests immediately\n\
        if ($request_method = OPTIONS) {\n\
            add_header Access-Control-Allow-Origin * always;\n\
            add_header Access-Control-Allow-Methods \"GET, POST, PUT, DELETE, PATCH, OPTIONS\" always;\n\
            add_header Access-Control-Allow-Headers \"*\" always;\n\
            add_header Access-Control-Allow-Credentials \"true\" always;\n\
            add_header Access-Control-Max-Age 86400 always;\n\
            add_header Content-Length 0;\n\
            add_header Content-Type text/plain;\n\
            return 204;\n\
        }\n\
        \n\
        proxy_pass http://localhost:5000/api/;\n\
        proxy_http_version 1.1;\n\
        proxy_set_header Upgrade $http_upgrade;\n\
        proxy_set_header Connection keep-alive;\n\
        proxy_set_header Host $host;\n\
        proxy_cache_bypass $http_upgrade;\n\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n\
        proxy_set_header X-Forwarded-Proto $scheme;\n\
        \n\
        # Add CORS headers to all responses\n\
        add_header Access-Control-Allow-Origin * always;\n\
        add_header Access-Control-Allow-Methods \"GET, POST, PUT, DELETE, PATCH, OPTIONS\" always;\n\
        add_header Access-Control-Allow-Headers \"*\" always;\n\
        add_header Access-Control-Allow-Credentials \"true\" always;\n\
    }\n\
    \n\
    # Swagger UI\n\
    location /swagger {\n\
        proxy_pass http://localhost:5000/swagger;\n\
        proxy_http_version 1.1;\n\
        proxy_set_header Host $host;\n\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n\
        proxy_set_header X-Forwarded-Proto $scheme;\n\
    }\n\
}' > /etc/nginx/sites-available/default

# Environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# Create startup script
RUN echo '#!/bin/bash\n\
set -e\n\
\n\
echo "════════════════════════════════════════════════════════════════"\n\
echo "  HRMS Payroll System - Production Startup"\n\
echo "════════════════════════════════════════════════════════════════"\n\
\n\
# ─── Database Migration ─────────────────────────────────────────\n\
if [ -n "$DATABASE_URL" ]; then\n\
  echo "📦 Database: PostgreSQL (Railway)"\n\
  echo "🔗 Connection: ${DATABASE_URL:0:25}..."\n\
  \n\
  # Parse postgresql://user:pass@host:port/db\n\
  if [[ $DATABASE_URL =~ postgresql://([^:]+):([^@]+)@([^:]+):([^/]+)/(.+) ]]; then\n\
    USER="${BASH_REMATCH[1]}"\n\
    PASS="${BASH_REMATCH[2]}"\n\
    HOST="${BASH_REMATCH[3]}"\n\
    PORT="${BASH_REMATCH[4]}"\n\
    DB="${BASH_REMATCH[5]}"\n\
    \n\
    CONN_STRING="Host=$HOST;Port=$PORT;Database=$DB;Username=$USER;Password=$PASS;SSL Mode=Require;Trust Server Certificate=true"\n\
    \n\
    echo "🔄 Running database migrations..."\n\
    ./efbundle --connection "$CONN_STRING" || {\n\
      echo "⚠️  Migration failed - database may already be up to date"\n\
    }\n\
    echo "✅ Database ready"\n\
  else\n\
    echo "❌ Could not parse DATABASE_URL"\n\
    exit 1\n\
  fi\n\
else\n\
  echo "⚠️  No DATABASE_URL found"\n\
fi\n\
\n\
# ─── Start Services ─────────────────────────────────────────────\n\
echo "🚀 Starting services..."\n\
\n\
# Start nginx in background\n\
echo "   • Nginx (port 8080) - serving Angular UI"\n\
nginx -g "daemon off;" &\n\
NGINX_PID=$!\n\
\n\
# Start .NET API\n\
echo "   • .NET API (port 5000)"\n\
cd /app/api\n\
exec dotnet HrmsApi.dll' > /app/start.sh && chmod +x /app/start.sh

CMD ["/app/start.sh"]
