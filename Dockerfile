# Use .NET 8 SDK for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore dependencies
COPY HrmsApi/*.csproj ./HrmsApi/
RUN cd HrmsApi && dotnet restore

# Copy everything else and build
COPY HrmsApi/. ./HrmsApi/
WORKDIR /app/HrmsApi
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /app

# Copy published app
COPY --from=build /app/HrmsApi/out .

# Copy entrypoint script
COPY HrmsApi/entrypoint.sh /app/
RUN chmod +x /app/entrypoint.sh

# Install EF Core tools for migrations
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Expose port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

# Start application with entrypoint
ENTRYPOINT ["/app/entrypoint.sh"]
