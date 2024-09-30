# Используем базовый образ с .NET SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Устанавливаем рабочую директорию
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["diary/diary.csproj", "diary/"]
RUN dotnet restore "./diary/diary.csproj"

# Копируем все файлы проекта
COPY . .

# Собираем проект
WORKDIR "/src/diary"
RUN dotnet build "./diary.csproj" -c Release -o /app/build

# Публикуем проект
FROM build AS publish
RUN dotnet publish "./diary.csproj" -c Release -o /app/publish

# Используем базовый образ с .NET Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем опубликованные файлы из предыдущего этапа
COPY --from=publish /app/publish .

# Создаем директорию для ключей и устанавливаем права
RUN mkdir -p /app/keys && chmod -R 755 /app/keys

# Указываем порты, которые будет слушать приложение
EXPOSE 8080
EXPOSE 8081

# Запускаем приложение
ENTRYPOINT ["dotnet", "diary.dll"]