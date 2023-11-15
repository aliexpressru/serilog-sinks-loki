FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 5000

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["samples/AspNetCore.WebApi/WebApi.csproj", "samples/AspNetCore.WebApi/"]
COPY ["src/Aerx.Serilog.Sinks.Loki/Aerx.Serilog.Sinks.Loki.csproj", "src/Aerx.Serilog.Sinks.Loki/"]
RUN dotnet restore "samples/AspNetCore.WebApi/WebApi.csproj"
COPY . .
WORKDIR "/src/samples/AspNetCore.WebApi"
RUN dotnet build "WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "WebApi.dll"]
