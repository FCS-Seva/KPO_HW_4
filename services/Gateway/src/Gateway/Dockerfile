FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY services/Gateway/src/Gateway/Gateway.csproj services/Gateway/src/Gateway/
RUN dotnet restore services/Gateway/src/Gateway/Gateway.csproj

COPY services/Gateway/src/Gateway/ services/Gateway/src/Gateway/
RUN dotnet publish services/Gateway/src/Gateway/Gateway.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Gateway.dll"]
