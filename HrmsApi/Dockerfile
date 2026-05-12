# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore
COPY *.csproj ./
RUN dotnet restore

# Copy everything else and build
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy published files
COPY --from=build /app/publish .

# Install EF tools for migrations
RUN dotnet tool install --global dotnet-ef --version 8.0.0
ENV PATH="${PATH}:/root/.dotnet/tools"

# Environment
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Startup script
RUN echo '#!/bin/bash\n\
set -e\n\
echo "Starting HRMS API..."\n\
sleep 3\n\
echo "Running migrations..."\n\
dotnet ef database update --no-build || true\n\
echo "Starting app..."\n\
exec dotnet HrmsApi.dll' > start.sh && chmod +x start.sh

CMD ["./start.sh"]
