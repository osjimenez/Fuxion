﻿FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Lab/Cloud/GATE/Fuxion.Lab.Cloud.GATE.csproj", "Lab/Cloud/GATE/"]
RUN dotnet restore "Lab/Cloud/GATE/Fuxion.Lab.Cloud.GATE.csproj"
COPY . .
WORKDIR "/src/Lab/Cloud/GATE"
RUN dotnet build "Fuxion.Lab.Cloud.GATE.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Fuxion.Lab.Cloud.GATE.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Fuxion.Lab.Cloud.GATE.dll"]
