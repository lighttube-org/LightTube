FROM  mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM --platform=$BUILDPLATFORM  mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG TARGETARCH
WORKDIR /src
COPY ["LightTube/LightTube.csproj", "LightTube/"]
RUN dotnet restore -a $TARGETARCH "LightTube/LightTube.csproj"
COPY . .
WORKDIR "/src/LightTube"
RUN dotnet build "LightTube.csproj" -a $TARGETARCH -c Release -o /app/build /p:Version=`date +0.%Y.%m.%d`

FROM build AS publish
RUN dotnet publish "LightTube.csproj" -a $TARGETARCH -c Release -o /app/publish /p:Version=`date +0.%Y.%m.%d`

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
RUN chmod 777 -R /tmp && chmod o+t -R /tmp
CMD ASPNETCORE_URLS=http://*:$PORT dotnet LightTube.dll