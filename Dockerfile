FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY HabitTrackerWeb.csproj ./
RUN dotnet restore HabitTrackerWeb.csproj

COPY . ./
RUN dotnet publish HabitTrackerWeb.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

COPY --from=build /app/publish ./

EXPOSE 10000
ENTRYPOINT ["dotnet", "HabitTrackerWeb.dll"]
