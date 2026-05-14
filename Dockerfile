# ===================================
# HRMS API - Multi-stage Dockerfile
# Optimized for Railway deployment
# ===================================

# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file and restore dependencies
COPY HrmsApi/HrmsApi.csproj HrmsApi/
RUN dotnet restore "HrmsApi/HrmsApi.csproj"

# Copy all source code
COPY HrmsApi/ HrmsApi/

# Build the application
WORKDIR /src/HrmsApi
RUN dotnet build "HrmsApi.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "HrmsApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Install PostgreSQL client tools (optional, for debugging)
RUN apt-get update && apt-get install -y postgresql-client && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Expose port (Railway assigns PORT dynamically)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "HrmsApi.dll"]
