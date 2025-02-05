using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using diary.Models;

namespace diary.Services
{
    public class TypeWeekDownloadService : IHostedService
    {
        private static TimeSpan CheckInterval = TimeSpan.FromHours(6);
        private CancellationTokenSource _cts;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<ScheduleOptions> _scheduleOptions;

        public TypeWeekDownloadService(IConfiguration configuration, IOptionsMonitor<ScheduleOptions> scheduleOptions)
        {
            _configuration = configuration;
            _scheduleOptions = scheduleOptions;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Запуск цикла в отдельной задаче
            _ = DownloadTypeWeek(_cts.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts.Cancel();

            return Task.CompletedTask;
        }

        private async Task DownloadTypeWeek(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    

                    using var httpClient = new HttpClient();

                    var response = await httpClient.GetAsync("https://www.smtu.ru/ru/listschedule/", cancellationToken);

                    var html = await response.Content.ReadAsStringAsync(cancellationToken);
                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    string xpath = "//h4[contains(.,'Сегодня')]";
                    HtmlNode node = doc.DocumentNode.SelectSingleNode(xpath);

                    if (node == null)
                    {
                        Console.WriteLine("⚠ XPath-узел не найден! Проверьте структуру страницы.");
                        await Task.Delay(CheckInterval, cancellationToken);
                        continue;
                    }

                    string text = node.InnerText.Trim();
                    string[] parts = text.Split(',');

                    if (parts.Length < 3)
                    {
                        Console.WriteLine("⚠ Ошибка: Некорректный формат строки.");
                        await Task.Delay(CheckInterval, cancellationToken);
                        continue;
                    }

                    string value = parts[2].Trim();
                    value = char.ToUpper(value[0]) + value.Substring(1);

                    Console.WriteLine($"✅ Обновлено значение TypeWeek: {value}");

                    // Обновляем в ScheduleOptions
                    _scheduleOptions.CurrentValue.TypeWeek = value;

                    // Сохраняем изменения в appsettings.json
                    var configPath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");

                    if (File.Exists(configPath))
                    {
                        var configJson = await File.ReadAllTextAsync(configPath, cancellationToken);
                        var configDoc = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(configJson);
                        configDoc["ScheduleOptions"]["TypeWeek"] = value;
                        var updatedConfigJson = Newtonsoft.Json.JsonConvert.SerializeObject(configDoc, Newtonsoft.Json.Formatting.Indented);
                        await File.WriteAllTextAsync(configPath, updatedConfigJson, cancellationToken);
                    }
                    else
                    {
                        Console.WriteLine($"⚠ Ошибка: файл appsettings.json не найден по пути {configPath}");
                    }

                    await Task.Delay(CheckInterval, cancellationToken);
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"❌ Ошибка при HTTPS-запросе: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"❌ Неизвестная ошибка: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(5), cancellationToken);
                }
            }
        }

    }
}
