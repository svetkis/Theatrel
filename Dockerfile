FROM mcr.microsoft.com/dotnet/core/runtime:3.1-buster-slim AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["theatrel.Console/theatrel.Console.csproj", "theatrel.Console/"]
COPY ["Theatrel.Interfaces/theatrel.Interfaces.csproj", "Theatrel.Interfaces/"]
COPY ["theatrel.Lib/theatrel.Lib.csproj", "theatrel.Lib/"]
COPY ["theatrel.Tests/theatrel.Tests.csproj", "theatrel.Tests/"]
COPY [theatrel.TLBot/theatrel.TLBot.csproj", "theatrel.TLBot/"]
COPY ["theatrel.TLBot.Interfaces/theatrel.TLBot.Interfaces.csproj", "theatrel.TLBot.Interfaces/"]
COPY ["theatrel.TLBot.Tests/theatrel.TLBot.Tests.csproj", "theatrel.TLBot.Tests/"]
COPY ["theatrel.Worker/theatrel.Worker.csproj", "theatrel.Worker/"]
RUN dotnet restore "theatrel.Worker/theatrel.Worker.csproj"
COPY . .
WORKDIR "/src/theatrel.Worker"
RUN dotnet build "theatrel.Worker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "theatrel.Worker.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "theatrel.Worker.dll"]