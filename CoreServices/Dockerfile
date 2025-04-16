FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["CoreServices/CoreServices.csproj", "CoreServices/"]
RUN dotnet restore "CoreServices/CoreServices.csproj"
COPY . .
WORKDIR "/src/CoreServices"
RUN dotnet build "CoreServices.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "CoreServices.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "CoreServices.dll"]

LABEL org.opencontainers.image.source=https://github.com/jeffu231/AmateurRadioServices
LABEL org.opencontainers.image.description="Amateur Radio Services - Core services for Amateur Radio"
LABEL org.opencontainers.image.licenses=MIT
