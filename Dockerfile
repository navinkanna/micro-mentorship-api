FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MicroMentorshipAPI/MicroMentorshipAPI.csproj MicroMentorshipAPI/
RUN dotnet restore MicroMentorshipAPI/MicroMentorshipAPI.csproj

COPY . .
RUN dotnet publish MicroMentorshipAPI/MicroMentorshipAPI.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MicroMentorshipAPI.dll"]
