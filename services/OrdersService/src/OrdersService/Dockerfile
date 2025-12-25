FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY shared/Shared.Contracts/Shared.Contracts.csproj shared/Shared.Contracts/
COPY services/OrdersService/src/OrdersService/OrdersService.csproj services/OrdersService/src/OrdersService/
RUN dotnet restore services/OrdersService/src/OrdersService/OrdersService.csproj

COPY shared/Shared.Contracts/ shared/Shared.Contracts/
COPY services/OrdersService/src/OrdersService/ services/OrdersService/src/OrdersService/
RUN dotnet publish services/OrdersService/src/OrdersService/OrdersService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "OrdersService.dll"]
