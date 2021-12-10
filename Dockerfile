FROM mcr.microsoft.com/dotnet/runtime:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["theatrel.Worker/theatrel.Worker.csproj", "theatrel.Worker/"]
COPY ["theatrel.sln", "./"]
RUN dotnet restore "theatrel.Worker/theatrel.Worker.csproj"
COPY . .
WORKDIR "/src"
RUN dotnet build "theatrel.sln" -c Release -o /app/build

FROM build AS publish
WORKDIR "/src/theatrel.Worker"
RUN dotnet publish "theatrel.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "theatrel.Worker.dll"]