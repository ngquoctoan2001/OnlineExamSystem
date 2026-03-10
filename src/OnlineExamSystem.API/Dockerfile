# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files for layer caching
COPY OnlineExamSystem.sln ./
COPY src/OnlineExamSystem.Domain/OnlineExamSystem.Domain.csproj src/OnlineExamSystem.Domain/
COPY src/OnlineExamSystem.Application/OnlineExamSystem.Application.csproj src/OnlineExamSystem.Application/
COPY src/OnlineExamSystem.Infrastructure/OnlineExamSystem.Infrastructure.csproj src/OnlineExamSystem.Infrastructure/
COPY src/OnlineExamSystem.API/OnlineExamSystem.API.csproj src/OnlineExamSystem.API/

# Restore packages
RUN dotnet restore src/OnlineExamSystem.API/OnlineExamSystem.API.csproj

# Copy all source code
COPY . .

# Publish release build
RUN dotnet publish src/OnlineExamSystem.API/OnlineExamSystem.API.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN addgroup --system appgroup && adduser --system --ingroup appgroup appuser

COPY --from=build /app/publish .

# Set environment
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

USER appuser
EXPOSE 8080

ENTRYPOINT ["dotnet", "OnlineExamSystem.API.dll"]
