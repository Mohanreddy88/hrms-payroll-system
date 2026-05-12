# Use .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY HrmsApi/*.csproj ./HrmsApi/
RUN cd HrmsApi && dotnet restore

# Copy everything else and build
COPY HrmsApi/. ./HrmsApi/
WORKDIR /app/HrmsApi
RUN dotnet publish -c Release -o /app/publish

# Build runtime image with SDK (needed for EF migrations)
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy published app
COPY --from=build /app/publish .

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="$PATH:/root/.dotnet/tools"

# Create startup script inline
RUN echo '#!/bin/bash' > /app/start.sh && \
    echo 'set -e' >> /app/start.sh && \
    echo 'echo "Starting HRMS API..."' >> /app/start.sh && \
    echo 'echo "Waiting for database..."' >> /app/start.sh && \
    echo 'sleep 5' >> /app/start.sh && \
    echo 'echo "Running migrations..."' >> /app/start.sh && \
    echo 'dotnet ef database update --no-build || echo "Migrations done or failed"' >> /app/start.sh && \
    echo 'echo "Starting application..."' >> /app/start.sh && \
    echo 'exec dotnet HrmsApi.dll' >> /app/start.sh && \
    chmod +x /app/start.sh

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start application
ENTRYPOINT ["/app/start.sh"]
