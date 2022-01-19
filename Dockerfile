FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["LightTube/LightTube.csproj", "LightTube/"]
RUN dotnet restore "LightTube/LightTube.csproj"
COPY . .
WORKDIR "/src/LightTube"
RUN dotnet build "LightTube.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "LightTube.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "LightTube.dll"]
