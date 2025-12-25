FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/Frontend/src/Frontend/Frontend.csproj services/Frontend/src/Frontend/
RUN dotnet restore services/Frontend/src/Frontend/Frontend.csproj

COPY services/Frontend/src/Frontend/ services/Frontend/src/Frontend/
RUN dotnet publish services/Frontend/src/Frontend/Frontend.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Frontend.dll"]
