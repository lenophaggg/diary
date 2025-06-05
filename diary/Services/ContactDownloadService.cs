using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using diary.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using diary.Data;
using Microsoft.Extensions.Logging;

namespace diary.Services
{
    public class ContactDownloadService : IHostedService, IDisposable
    {
        private CancellationTokenSource _cts;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ContactDownloadService> _logger;
        private readonly HttpClient _httpClient;
        private Task _executingTask;

        public ContactDownloadService(IServiceProvider serviceProvider, ILogger<ContactDownloadService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactDownloadService стартует, текущее время: {Now}", DateTime.Now);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _executingTask = ExecuteAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactDownloadService останавливается...");
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Первый запуск сразу
            await RunContactDownloadSafe(cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                var now = DateTime.Now;
                DateTime nextRunTime = now.Date.AddDays(1).AddHours(4);
                TimeSpan delay = nextRunTime - now;

                _logger.LogInformation("Следующий запуск парсинга контактов запланирован на {NextRunTime}, через {Seconds} секунд",
                    nextRunTime, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break; // Остановка по требованию
                }

                await RunContactDownloadSafe(cancellationToken);
            }
        }

        private async Task RunContactDownloadSafe(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Начинаем парсинг подразделений (RunContactDownload) в {Time}", DateTime.Now);
            try
            {
                await RunContactDownload(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RunContactDownload: критичная ошибка парсинга");
            }
        }

        private async Task RunContactDownload(CancellationToken cancellationToken)
        {
            try
            {
                await ParseAndSavePersonContact();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при ParseAndSavePersonContact");
            }

            try
            {
                await UpdatePersonContacts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при UpdatePersonContacts");
            }
        }

        private async Task ParseAndSavePersonContact()
        {
            List<string> listDepartment;
            try
            {
                listDepartment = await ParseDepartment("https://www.smtu.ru/ru/listdepartment/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка получения списка подразделений");
                return;
            }
            foreach (var departmentLink in listDepartment)
            {
                try
                {
                    await ParseUnitEmployeesAndSaveNameUniversityIdEmailPhoneImg(departmentLink);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка обработки подразделения: {DepartmentLink}", departmentLink);
                }
            }
        }

        private async Task<List<string>> ParseDepartment(string universityUrl)
        {
            var web = new HtmlWeb();
            var doc = await Task.Run(() => web.Load(universityUrl));
            var sectionNode = doc.DocumentNode
                .SelectSingleNode("//section[contains(@class, 'bg-body-secondary') and contains(@class, 'pb-5')]");

            if (sectionNode == null)
            {
                _logger.LogWarning("Не найдена секция bg-body-secondary pb-5 на {Url}", universityUrl);
                return new List<string>();
            }

            var unitLinks = sectionNode.SelectNodes(".//a[@href]")
                ?.Select(a => a.GetAttributeValue("href", "").Trim())
                .Where(href => href.Contains("/ru/viewunit/"))
                .Distinct()
                .ToList();

            return unitLinks ?? new List<string>();
        }

        private async Task ParseUnitEmployeesAndSaveNameUniversityIdEmailPhoneImg(string unitLink)
        {
            var url = $"https://www.smtu.ru{unitLink}";
            var web = new HtmlWeb();
            var doc = await Task.Run(() => web.Load(url));

            var cardDivs = doc.DocumentNode.SelectNodes("//div[@id='faculty-per-content']//div[contains(@class, 'card') and contains(@class, 'bg-body-tertiary')]");
            if (cardDivs == null || cardDivs.Count == 0)
            {
                _logger.LogInformation("На странице {Url} не найдены карточки сотрудников", url);
                return;
            }

            foreach (var cardDiv in cardDivs)
            {
                string name = cardDiv.SelectSingleNode(".//h4[@class='h6 text-info-dark']/a")?.InnerText.Trim()
                           ?? cardDiv.SelectSingleNode(".//h4[@class='h6 text-info-dark']")?.InnerText.Trim();

                if (string.IsNullOrWhiteSpace(name))
                    continue;

                var imgNode = cardDiv.SelectSingleNode(".//img");
                string imgSrc = imgNode?.GetAttributeValue("src", "")?.Split('?')[0];
                string personId = null;
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    var match = Regex.Match(imgSrc, @"p(\d+)\.jpg$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        personId = match.Groups[1].Value;
                    }
                }

                var listItems = cardDiv.SelectNodes(".//ul[@class='fa-ul']/li");
                var positions = new List<string>();
                var academicTitles = new List<string>();

                if (listItems != null)
                {
                    foreach (var li in listItems)
                    {
                        if (li.InnerHtml.Contains("fa-briefcase"))
                            positions.Add(li.InnerText.Trim());
                        else if (li.InnerHtml.Contains("fa-user-graduate"))
                            academicTitles.Add(li.InnerText.Trim());
                    }
                }

                string finalImgPath = null;
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    string bigImgPath = imgSrc.Contains("/small/") ? imgSrc.Replace("/small/", "/big/") : imgSrc;
                    string smallImgPath = imgSrc.Contains("/big/") ? imgSrc.Replace("/big/", "/small/") : imgSrc;

                    try
                    {
                        var response = await _httpClient.GetAsync($"https://isu.smtu.ru{bigImgPath}", HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                            finalImgPath = bigImgPath;
                        else
                            finalImgPath = smallImgPath;
                    }
                    catch
                    {
                        finalImgPath = smallImgPath;
                    }
                }

                var personData = new PersonContactData
                {
                    UniversityIdContact = personId,
                    NameContact = name,
                    Position = positions.ToArray(),
                    AcademicDegree = academicTitles.Count > 0 ? string.Join("; ", academicTitles) : null,
                    ImgPath = finalImgPath
                };

                if (string.IsNullOrEmpty(personId))
                {
                    _logger.LogWarning("Для {Name} не удалось извлечь ID (страница {Url}). Пропускаем...", name, url);
                    continue;
                }

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                        var existing = await context.PersonContacts
                            .FirstOrDefaultAsync(x => x.UniversityIdContact == personId);

                        if (existing != null)
                        {
                            existing.NameContact = personData.NameContact;
                            existing.Position = personData.Position;
                            existing.AcademicDegree = personData.AcademicDegree;
                            existing.ImgPath = personData.ImgPath;
                        }
                        else
                        {
                            await context.PersonContacts.AddAsync(personData);
                        }

                        await context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при сохранении сотрудника {Name} (ID={Id})", name, personId);
                }
            }
        }

        private async Task UpdatePersonContacts()
        {
            _logger.LogInformation("Запуск UpdatePersonContacts...");
            var web = new HtmlWeb();

            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var contacts = await context.PersonContacts.ToListAsync();

                foreach (var personContact in contacts)
                {
                    if (string.IsNullOrEmpty(personContact.UniversityIdContact))
                        continue;

                    var url = $"https://isu.smtu.ru/view_user_page/{personContact.UniversityIdContact}/";
                    var htmlDoc = await Task.Run(() => web.Load(url));
                    var mainNode = htmlDoc.DocumentNode
                        .SelectSingleNode("//div[@class='warper container-fluid']/div[@class='panel panel-default']");

                    if (mainNode == null)
                        continue;

                    var positionNodes = mainNode.SelectNodes(".//div[@itemprop='post']/li");
                    personContact.Position = positionNodes?.Select(n => n.InnerText.Trim()).ToArray();

                    var taughtSubjectsNodes = mainNode.SelectNodes(".//div[@itemprop='teachingDiscipline']/li");
                    var taughtSubjects = taughtSubjectsNodes?.Select(n => n.InnerText.Trim()).ToList();
                    if (taughtSubjects != null)
                    {
                        foreach (var subjectName in taughtSubjects)
                        {
                            try
                            {
                                await AddOrUpdateSubject(context, personContact, subjectName);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Ошибка AddOrUpdateSubject: {Subject} для {Person}", subjectName, personContact.UniversityIdContact);
                            }
                        }
                    }

                    var academicDegreeNode = mainNode.SelectSingleNode(".//div[@itemprop='degree']/li");
                    personContact.AcademicDegree = academicDegreeNode?.InnerText?.Trim();

                    var teachingExperienceNode = mainNode.SelectSingleNode(".//div[@itemprop='specExperience']/li");
                    personContact.TeachingExperience = teachingExperienceNode?.InnerText?.Trim();

                    try
                    {
                        await context.SaveChangesAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Ошибка при сохранении подробных данных сотрудника {Person}", personContact.UniversityIdContact);
                    }
                }
            }
        }

        private async Task AddOrUpdateSubject(ApplicationDbContext context, PersonContactData personContact, string subjectName)
        {
            var existingSubject = await context.Subjects.FirstOrDefaultAsync(s => s.SubjectName == subjectName);
            if (existingSubject == null)
            {
                try
                {
                    var subject = new SubjectData { SubjectName = subjectName };
                    context.Subjects.Add(subject);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при добавлении предмета {Subject}", subjectName);
                }
            }

            var existingPersonTaughtSubject = await context.PersonTaughtSubjects
                .FirstOrDefaultAsync(pts => pts.IdContact == personContact.IdContact && pts.SubjectName == subjectName);

            if (existingPersonTaughtSubject == null)
            {
                try
                {
                    var personTaughtSubject = new PersonTaughtSubjectData
                    {
                        IdContact = personContact.IdContact,
                        SubjectName = subjectName
                    };
                    context.PersonTaughtSubjects.Add(personTaughtSubject);
                    await context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при связывании преподавателя {Person} и предмета {Subject}", personContact.UniversityIdContact, subjectName);
                }
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _httpClient?.Dispose();
        }
    }
}
