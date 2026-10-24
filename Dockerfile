FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ShortLinkDemo.slnx ./
COPY ApiHost/ApiHost.csproj ApiHost/
COPY Core/Application/Application.csproj Core/Application/
COPY Core/Domain/Domain.csproj Core/Domain/
COPY Data/Data.csproj Data/

RUN dotnet restore ApiHost/ApiHost.csproj

COPY ApiHost/ ApiHost/
COPY Core/ Core/
COPY Data/ Data/

RUN dotnet publish ApiHost/ApiHost.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

COPY --from=build /app/publish .

USER app
ENTRYPOINT ["dotnet", "ApiHost.dll"]
