FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["RAG.Backend.sln", "./"]
COPY ["src/RAG.Domain/RAG.Domain.csproj", "src/RAG.Domain/"]
COPY ["src/RAG.Application/RAG.Application.csproj", "src/RAG.Application/"]
COPY ["src/RAG.Infrastructure/RAG.Infrastructure.csproj", "src/RAG.Infrastructure/"]
COPY ["src/RAG.Api/RAG.Api.csproj", "src/RAG.Api/"]
RUN dotnet restore
COPY ["src/", "src/"]
WORKDIR "/src/src/RAG.Api"
RUN dotnet build "RAG.Api.csproj" -c Release -o /app/build
FROM build AS publish
RUN dotnet publish "RAG.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "RAG.Api.dll"]
