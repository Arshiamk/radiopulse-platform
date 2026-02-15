FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore src/RadioPulse.Web/RadioPulse.Web.csproj
RUN dotnet publish src/RadioPulse.Web/RadioPulse.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENV ASPNETCORE_URLS=http://+:8081
USER $APP_UID
ENTRYPOINT ["dotnet", "RadioPulse.Web.dll"]
