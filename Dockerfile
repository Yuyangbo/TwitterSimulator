FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["TwitterWeb.fsproj", "./"]
RUN dotnet restore "TwitterWeb.fsproj"
COPY . .
WORKDIR "/src/"
RUN dotnet build "TwitterWeb.fsproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TwitterWeb.fsproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TwitterWeb.dll"]
