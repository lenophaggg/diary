using HtmlAgilityPack;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using diary.Models;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace diary.Services
{
    public class TypeWeekDownloadService : IHostedService
    {
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
        private CancellationTokenSource _cts;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<ScheduleOptions> _scheduleOptions;
        private readonly ILogger<TypeWeekDownloadService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;

        public TypeWeekDownloadService(
            IConfiguration configuration,
            IOptionsMonitor<ScheduleOptions> scheduleOptions,
            ILogger<TypeWeekDownloadService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _configuration = configuration;
            _scheduleOptions = scheduleOptions;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _logger.LogInformation("TypeWeekDownloadService запущен. Первый запуск через несколько секунд...");
            _ = DownloadTypeWeek(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();
            _logger.LogInformation("TypeWeekDownloadService остановлен.");
            return Task.CompletedTask;
        }

        private async Task DownloadTypeWeek(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    string url = "https://www.smtu.ru/ru/listschedule/";
                    _logger.LogInformation("Отправляю запрос по URL: {Url}", url);

                    var client = _httpClientFactory.CreateClient("Smtu");
                    var response = await client.GetAsync(url, cancellationToken);

                    _logger.LogInformation("Ответ получен: {StatusCode}", response.StatusCode);

                    var html = await response.Content.ReadAsStringAsync(cancellationToken);


                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    string xpath = "//h4[contains(.,'Сегодня')]";
                    _logger.LogInformation("Ищу узел по XPath: {XPath}", xpath);
                    HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);

                    if (node == null)
                    {
                        _logger.LogWarning("⚠ XPath-узел не найден! Проверьте структуру страницы.");
                        await Task.Delay(CheckInterval, cancellationToken);
                        continue;
                    }

                    string text = node.InnerText.Trim();
                    _logger.LogInformation("Текст из узла: {Text}", text);

                    string[] parts = text.Split(',');

                    if (parts.Length < 3)
                    {
                        _logger.LogWarning("⚠ Некорректный формат строки: {Text}", text);
                        await Task.Delay(CheckInterval, cancellationToken);
                        continue;
                    }

                    string value = parts[2].Trim();
                    value = char.ToUpper(value[0]) + value.Substring(1);

                    _logger.LogInformation("✅ Обновлено значение TypeWeek: {Value}", value);

                    // Обновляем в ScheduleOptions
                    _scheduleOptions.CurrentValue.TypeWeek = value;

                    // Сохраняем изменения в appsettings.json
                    var currentDir = Directory.GetCurrentDirectory();
                    var configPath = Path.Combine(currentDir, "appsettings.json");
                    _logger.LogInformation("Рабочая директория: {CurrentDir}", currentDir);
                    _logger.LogInformation("Путь к конфигу: {ConfigPath}", configPath);

                    if (File.Exists(configPath))
                    {
                        try
                        {
                            _logger.LogInformation("Файл appsettings.json найден. Читаю...");

                            var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
                            dynamic configDoc = Newtonsoft.Json.JsonConvert.DeserializeObject(configJson);

                            configDoc["ScheduleOptions"]["TypeWeek"] = value;

                            var updatedConfigJson = Newtonsoft.Json.JsonConvert.SerializeObject(configDoc, Newtonsoft.Json.Formatting.Indented);

                            // Пробуем сохранить изменения
                            try
                            {
                                await File.WriteAllTextAsync(configPath, updatedConfigJson, cancellationToken);
                                _logger.LogInformation("Файл appsettings.json успешно обновлён.");
                            }
                            catch (Exception exWrite)
                            {
                                _logger.LogWarning(exWrite, "Ошибка при записи в appsettings.json: {Message}", exWrite.Message);
                            }
                        }
                        catch (Exception exRead)
                        {
                            _logger.LogError(exRead, "Ошибка при чтении/обновлении appsettings.json: {Message}", exRead.Message);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠ Файл appsettings.json не найден по пути {ConfigPath}", configPath);
                    }
                }
                catch (HttpRequestException ex)
                {
                    _logger.LogError(ex, "❌ Ошибка при HTTPS-запросе: {Message}", ex.Message);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Неизвестная ошибка: {Message}", ex.Message);
                }

                // Ждём до следующей попытки
                _logger.LogInformation("Следующая попытка через {Hours} часов.", CheckInterval.TotalHours);
                await Task.Delay(CheckInterval, cancellationToken);
            }
        }
    }
}
