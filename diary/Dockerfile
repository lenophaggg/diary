
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

<<<<<<< HEAD:diary/Dockerfile
=======
EXPOSE 8080
EXPOSE 8081

>>>>>>> 4bae6fef3638e403f1d017b22ce6b669759087b8:Dockerfile
ENTRYPOINT ["dotnet", "diary.dll"]
