# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY LostAndFoundSystem.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# Persist SQLite + uploads on a volume
RUN mkdir -p /data /app/wwwroot/uploads && chmod -R 777 /data /app/wwwroot/uploads

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/data/lostandfound.db"

EXPOSE 8080

ENTRYPOINT ["dotnet", "LostAndFoundSystem.dll"]
