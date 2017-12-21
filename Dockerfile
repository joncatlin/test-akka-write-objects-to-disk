FROM microsoft/aspnetcore-build:1.1 AS build-env
WORKDIR /app
COPY . .

#WORKDIR /app/ClientDataService.UnitTests
#RUN ["dotnet", "restore", "--source", "http://repo.concordservicing.com:8081/nexus/service/local/nuget/nuget-public/"]
#RUN ["dotnet", "test"]

WORKDIR /app/SnapshotStore
RUN ["dotnet", "restore", "--source", "http://repo.concordservicing.com:8081/nexus/service/local/nuget/nuget-public/"]
RUN dotnet publish -c Release -o out

# Build runtime image
FROM microsoft/aspnetcore:1.1
WORKDIR /app
COPY --from=build-env /app/ClientDataService/out/ .
ENTRYPOINT ["dotnet", "ClientDataService.dll"]

