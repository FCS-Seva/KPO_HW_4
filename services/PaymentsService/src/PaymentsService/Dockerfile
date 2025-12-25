FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY shared/Shared.Contracts/Shared.Contracts.csproj shared/Shared.Contracts/
COPY services/PaymentsService/src/PaymentsService/PaymentsService.csproj services/PaymentsService/src/PaymentsService/
RUN dotnet restore services/PaymentsService/src/PaymentsService/PaymentsService.csproj

COPY shared/Shared.Contracts/ shared/Shared.Contracts/
COPY services/PaymentsService/src/PaymentsService/ services/PaymentsService/src/PaymentsService/
RUN dotnet publish services/PaymentsService/src/PaymentsService/PaymentsService.csproj -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "PaymentsService.dll"]
