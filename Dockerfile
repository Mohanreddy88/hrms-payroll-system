# ═══════════════════════════════════════════════════════════════
# Multi-stage Dockerfile for HRMS Production Deployment
# Builds: .NET 8 API + Angular UI in a single container
# ═══════════════════════════════════════════════════════════════

# ───────────────────────────────────────────────────────────────
# Stage 1: Build Angular Frontend
# ───────────────────────────────────────────────────────────────
FROM node:20-alpine AS angular-build
WORKDIR /app/frontend

COPY hrms-ui/package*.json ./
RUN npm ci --legacy-peer-deps

COPY hrms-ui/ ./
RUN npm run build:prod

# ───────────────────────────────────────────────────────────────
# Stage 2: Build .NET API
# ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS dotnet-build
WORKDIR /src

COPY HrmsApi/*.csproj ./HrmsApi/
RUN dotnet restore HrmsApi/HrmsApi.csproj

COPY HrmsApi/ ./HrmsApi/
RUN dotnet publish HrmsApi/HrmsApi.csproj -c Release -o /app/publish

# ───────────────────────────────────────────────────────────────
# Stage 3: Runtime
# ───────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

RUN apt-get update && apt-get install -y nginx && rm -rf /var/lib/apt/lists/*

COPY --from=dotnet-build /app/publish ./api/
COPY --from=angular-build /app/frontend/dist/hrms-ui/browser ./wwwroot

# Write nginx config
RUN printf 'server {\n\
    listen 8080;\n\
    server_name _;\n\
    root /app/wwwroot;\n\
    index index.html;\n\
\n\
    location / {\n\
        try_files $uri $uri/ /index.html;\n\
    }\n\
\n\
    location /api/ {\n\
        proxy_pass http://127.0.0.1:5000/api/;\n\
        proxy_http_version 1.1;\n\
        proxy_set_header Upgrade $http_upgrade;\n\
        proxy_set_header Connection keep-alive;\n\
        proxy_set_header Host $host;\n\
        proxy_read_timeout 300;\n\
        proxy_connect_timeout 300;\n\
        proxy_send_timeout 300;\n\
        proxy_cache_bypass $http_upgrade;\n\
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;\n\
        proxy_set_header X-Forwarded-Proto $scheme;\n\
    }\n\
}\n' > /etc/nginx/sites-available/default

# Write startup script using printf for reliable newlines
RUN printf '#!/bin/bash\n\
set -e\n\
echo "=== HRMS Payroll System Starting ==="\n\
\n\
# Start nginx in background\n\
echo "[1/2] Starting Nginx on port 8080..."\n\
nginx -g "daemon off;" &\n\
NGINX_PID=$!\n\
\n\
# Start .NET API\n\
echo "[2/2] Starting .NET API on port 5000..."\n\
cd /app/api\n\
dotnet HrmsApi.dll &\n\
DOTNET_PID=$!\n\
\n\
# Wait for both processes\n\
wait $DOTNET_PID\n' > /app/start.sh && chmod +x /app/start.sh

ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
EXPOSE 8080

CMD ["/bin/bash", "/app/start.sh"]
