# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY ["src/RAG.Gateway/RAG.Gateway.csproj", "RAG.Gateway/"]
RUN dotnet restore "RAG.Gateway/RAG.Gateway.csproj"

# Copy everything else and build
COPY ["src/RAG.Gateway/", "RAG.Gateway/"]
WORKDIR "/src/RAG.Gateway"
RUN dotnet build "RAG.Gateway.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "RAG.Gateway.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copy published app
COPY --from=publish /app/publish .

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
  CMD curl --fail http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "RAG.Gateway.dll"]
