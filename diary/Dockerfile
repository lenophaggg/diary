
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["diary/diary.csproj", "diary/"]
RUN dotnet restore "diary/diary.csproj"
COPY . .
WORKDIR "/src/diary"
RUN dotnet build "diary.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "diary.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "diary.dll"]
