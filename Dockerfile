# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY src/StockTakingApp/StockTakingApp.csproj ./src/StockTakingApp/
RUN dotnet restore src/StockTakingApp/StockTakingApp.csproj

# Copy source code and build
COPY src/ ./src/
WORKDIR /src/src/StockTakingApp
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN groupadd -r appgroup && useradd -r -g appgroup appuser

# Create data directory for SQLite with proper permissions
RUN mkdir -p /app/data && chown -R appuser:appgroup /app/data

# Copy published app
COPY --from=build /app/publish .

# Change ownership of app files
RUN chown -R appuser:appgroup /app

# Switch to non-root user
USER appuser

# Expose port (Render uses PORT env variable)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/stocktaking.db"

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "StockTakingApp.dll"]
