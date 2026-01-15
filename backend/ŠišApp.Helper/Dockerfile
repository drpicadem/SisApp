FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["ŠišApp.Helper.csproj", "."]
RUN dotnet restore "./ŠišApp.Helper.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "ŠišApp.Helper.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ŠišApp.Helper.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ŠišApp.Helper.dll"]
