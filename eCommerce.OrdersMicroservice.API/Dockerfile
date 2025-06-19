FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["eCommerce.OrdersMicroservice.API/eCommerce.OrdersMicroservice.API.csproj", "eCommerce.OrdersMicroservice.API/"]
RUN dotnet restore "eCommerce.OrdersMicroservice.API/eCommerce.OrdersMicroservice.API.csproj"
COPY . .
WORKDIR "/src/eCommerce.OrdersMicroservice.API"
RUN dotnet build "eCommerce.OrdersMicroservice.API.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "eCommerce.OrdersMicroservice.API.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENV MONGODB_HOST=localhost
ENV MONGODB_PORT=27017
ENV MONGODB_DATABASE=OrdersDatabase
ENV UsersMicroserviceName=localhost
ENV UsersMicroservicePort=9090
ENV ProductsMicroserviceName=localhost
ENV ProductsMicroservicePort=8080
ENTRYPOINT ["dotnet", "eCommerce.OrdersMicroservice.API.dll"]
