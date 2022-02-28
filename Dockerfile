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
# this might be a bad way to install yt-dlp, but idc, if it works, it works
RUN apt update
RUN apt install python3 curl -y
RUN curl -L https://github.com/yt-dlp/yt-dlp/releases/latest/download/yt-dlp -o /bin/yt-dlp
RUN chmod a+rx /bin/yt-dlp
COPY --from=publish /app/publish .
CMD ASPNETCORE_URLS=http://*:$PORT dotnet LightTube.dll
