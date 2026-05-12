# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Build migrations in build stage
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"
RUN dotnet ef migrations bundle --self-contained -r linux-x64 -o /app/efbundle

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Copy EF migration bundle
COPY --from=build /app/efbundle ./efbundle

# Environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Startup script
RUN echo '#!/bin/bash\n\
set -e\n\
echo "Starting HRMS API..."\n\
sleep 3\n\
echo "Running migrations..."\n\
if [ -n "$DATABASE_URL" ]; then\n\
  echo "Using PostgreSQL connection from DATABASE_URL"\n\
  echo "DATABASE_URL format: ${DATABASE_URL:0:20}..."\n\
  # Parse postgresql://user:pass@host:port/db format\n\
  if [[ $DATABASE_URL =~ postgresql://([^:]+):([^@]+)@([^:]+):([^/]+)/(.+) ]]; then\n\
    USER="${BASH_REMATCH[1]}"\n\
    PASS="${BASH_REMATCH[2]}"\n\
    HOST="${BASH_REMATCH[3]}"\n\
    PORT="${BASH_REMATCH[4]}"\n\
    DB="${BASH_REMATCH[5]}"\n\
    CONN_STRING="Host=$HOST;Port=$PORT;Database=$DB;Username=$USER;Password=$PASS;SSL Mode=Require;Trust Server Certificate=true"\n\
    echo "Running migrations with parsed connection..."\n\
    ./efbundle --connection "$CONN_STRING" || echo "Migration failed, continuing..."\n\
  else\n\
    echo "Could not parse DATABASE_URL format"\n\
  fi\n\
else\n\
  echo "No DATABASE_URL found, skipping migrations"\n\
fi\n\
echo "Starting app..."\n\
exec dotnet HrmsApi.dll' > start.sh && chmod +x start.sh

CMD ["./start.sh"]
