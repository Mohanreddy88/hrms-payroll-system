# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project file
COPY ["HrmsApi/HrmsApi.csproj", "HrmsApi/"]

# Restore dependencies
RUN dotnet restore "HrmsApi/HrmsApi.csproj"

# Copy all source files
COPY HrmsApi/ HrmsApi/

# Build and publish
WORKDIR /src/HrmsApi
RUN dotnet publish "HrmsApi.csproj" -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Copy published output
COPY --from=build /app/publish .

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Create startup script that runs migrations
RUN echo '#!/bin/bash\n\
set -e\n\
echo "HRMS API Starting..."\n\
echo "Waiting for database connection..."\n\
sleep 5\n\
echo "Running database migrations..."\n\
dotnet ef database update --no-build || echo "Migrations already applied or failed"\n\
echo "Starting application..."\n\
exec dotnet HrmsApi.dll\n' > /app/start.sh && chmod +x /app/start.sh

# Start
CMD ["/bin/bash", "/app/start.sh"]
