FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["LightTube/LightTube.csproj", "LightTube/"]
RUN dotnet restore "LightTube/LightTube.csproj"
COPY . .
WORKDIR "/src/LightTube"
RUN dotnet build "LightTube.csproj" -c Release -o /app/build /p:Version=`date +0.%Y.%m.%d`

FROM build AS publish
RUN dotnet publish "LightTube.csproj" -c Release -o /app/publish /p:Version=`date +0.%Y.%m.%d`

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet LightTube.dll