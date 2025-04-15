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
            // Можно использовать IHttpClientFactory, но для примера создаём один HttpClient
            _httpClient = new HttpClient();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactDownloadService запускается...");
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Запускаем нашу фоновую задачу
            _executingTask = ExecuteAsync(_cts.Token);
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("ContactDownloadService останавливается...");
            _cts?.Cancel();
            return Task.CompletedTask;
        }

        /// <summary>
        /// Основной цикл: первый запуск сразу, потом каждый день в 4:00
        /// </summary>
        private async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            // Первый запуск (сразу после старта приложения)
            await RunContactDownload(cancellationToken);

            // Затем ежедневно в 4:00
            while (!cancellationToken.IsCancellationRequested)
            {
                DateTime now = DateTime.Now;
                DateTime nextRunTime = now.Date.AddDays(1).AddHours(4);
                TimeSpan delay = nextRunTime - now;

                _logger.LogInformation("Следующий запуск запланирован на {NextRunTime} (через {Seconds} сек.)",
                    nextRunTime, delay.TotalSeconds);

                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                await RunContactDownload(cancellationToken);
            }
        }

        /// <summary>
        /// Вызывается каждый раз, когда нужно запустить/обновить контакты
        /// </summary>
        private async Task RunContactDownload(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Начинаем парсинг подразделений (RunContactDownload) в {Time}", DateTime.Now);
            try
            {
                await ParseAndSavePersonContact();

                // После основного парсинга — обновляем детальную информацию (при необходимости)
                await UpdatePersonContacts();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка во время RunContactDownload");
            }
        }

        /// <summary>
        /// Парсим страницу /ru/listdepartment/, находим подразделения, идём по каждой и собираем сотрудников
        /// </summary>
        private async Task ParseAndSavePersonContact()
        {
            // Получаем ссылки подразделений
            var listDepartment = await ParseDepartment("https://www.smtu.ru/ru/listdepartment/");
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

        /// <summary>
        /// Из страницы /ru/listdepartment/ извлекаем ссылки /ru/viewunit/XXXX/
        /// </summary>
        private async Task<List<string>> ParseDepartment(string universityUrl)
        {
            var web = new HtmlWeb();
            var doc = await Task.Run(() => web.Load(universityUrl));

            // Новая секция: bg-body-secondary pb-5
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

        /// <summary>
        /// Заходим на каждую страницу /ru/viewunit/XXXX/, ищем section#staff_list -> .details_content -> .card
        /// и сохраняем сотрудников
        /// </summary>
        private async Task ParseUnitEmployeesAndSaveNameUniversityIdEmailPhoneImg(string unitLink)
        {
            var url = $"https://www.smtu.ru{unitLink}";
            var web = new HtmlWeb();
            var doc = await Task.Run(() => web.Load(url));

            // Получаем все карточки сотрудников из нового контейнера "faculty-per-content"
            var cardDivs = doc.DocumentNode.SelectNodes("//div[@id='faculty-per-content']//div[contains(@class, 'card') and contains(@class, 'bg-body-tertiary')]");
            if (cardDivs == null || cardDivs.Count == 0)
            {
                _logger.LogInformation("На странице {Url} не найдены карточки сотрудников", url);
                return;
            }

            foreach (var cardDiv in cardDivs)
            {
                // Извлекаем имя сотрудника
                var nameNode = cardDiv.SelectSingleNode(".//h4[@class='h6 text-info-dark']/a")
                               ?? cardDiv.SelectSingleNode(".//h4[@class='h6 text-info-dark']");
                if (nameNode == null)
                    continue;
                string name = nameNode.InnerText.Trim();

                // Извлекаем изображение и определяем ID сотрудника
                var imgNode = cardDiv.SelectSingleNode(".//img");
                string imgSrc = imgNode?.GetAttributeValue("src", "");
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    // Убираем параметры запроса, если они присутствуют (например, ?nocache=...)
                    imgSrc = imgSrc.Split('?')[0];
                }

                string personId = null;
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    // Пример: .../p107994.jpg => извлекаем 107994
                    var match = Regex.Match(imgSrc, @"p(\d+)\.jpg$", RegexOptions.IgnoreCase);
                    if (match.Success)
                    {
                        personId = match.Groups[1].Value;
                    }
                }

                // Извлекаем информацию о должностях и ученых степенях
                var listItems = cardDiv.SelectNodes(".//ul[@class='fa-ul']/li");
                var positions = new List<string>();
                var academicTitles = new List<string>();

                if (listItems != null)
                {
                    foreach (var li in listItems)
                    {
                        // Если в HTML содержится класс fa-briefcase, то считаем, что это должность
                        if (li.InnerHtml.Contains("fa-briefcase"))
                        {
                            positions.Add(li.InnerText.Trim());
                        }
                        // Если содержится класс fa-user-graduate, то это ученая степень
                        else if (li.InnerHtml.Contains("fa-user-graduate"))
                        {
                            academicTitles.Add(li.InnerText.Trim());
                        }
                    }
                }

                // Определяем путь к изображению
                string finalImgPath = null;
                if (!string.IsNullOrEmpty(imgSrc))
                {
                    string bigImgPath = imgSrc.Contains("/small/") ? imgSrc.Replace("/small/", "/big/") : imgSrc;
                    string smallImgPath = imgSrc.Contains("/big/") ? imgSrc.Replace("/big/", "/small/") : imgSrc;

                    // Проверяем доступность изображения /big/
                    try
                    {
                        var response = await _httpClient.GetAsync($"https://isu.smtu.ru{bigImgPath}", HttpCompletionOption.ResponseHeadersRead);
                        if (response.IsSuccessStatusCode)
                        {
                            finalImgPath = bigImgPath; // Если /big/ доступен, используем его
                        }
                        else
                        {
                            _logger.LogWarning("Изображение {BigImgPath} недоступно (статус: {StatusCode}), пробуем /small/", bigImgPath, response.StatusCode);
                            finalImgPath = smallImgPath; // Если /big/ вернул 404, используем /small/
                        }
                    }
                    catch (HttpRequestException ex)
                    {
                        _logger.LogWarning(ex, "Ошибка при проверке изображения {BigImgPath}, используем /small/", bigImgPath);
                        finalImgPath = smallImgPath; // В случае ошибки сети также используем /small/
                    }
                }

                // Формируем данные сотрудника
                var personData = new PersonContactData
                {
                    UniversityIdContact = personId,
                    NameContact = name,
                    Position = positions.ToArray(),
                    AcademicDegree = academicTitles.Count > 0 ? string.Join("; ", academicTitles) : null,
                    ImgPath = finalImgPath
                };

                // Если не удалось извлечь personId, логируем предупреждение и пропускаем запись
                if (string.IsNullOrEmpty(personId))
                {
                    _logger.LogWarning("Для {Name} не удалось извлечь ID (страница {Url}). Пропускаем...", name, url);
                    continue;
                }

                // Сохраняем данные в базу
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
                        // Обновляем другие поля при необходимости
                    }
                    else
                    {
                        await context.PersonContacts.AddAsync(personData);
                    }

                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Дополнительный метод: идём на https://isu.smtu.ru/view_user_page/{UniversityIdContact}/ и выкачиваем детали
        /// (например, преподаваемые предметы, email, телефон и др.)
        /// </summary>
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
                    // Если нет ID, бессмысленно идти на /view_user_page/xxx/
                    if (string.IsNullOrEmpty(personContact.UniversityIdContact))
                        continue;

                    var url = $"https://isu.smtu.ru/view_user_page/{personContact.UniversityIdContact}/";
                    var htmlDoc = await Task.Run(() => web.Load(url));
                    var mainNode = htmlDoc.DocumentNode
                        .SelectSingleNode("//div[@class='warper container-fluid']/div[@class='panel panel-default']");

                    if (mainNode == null)
                    {
                        _logger.LogInformation("Нет информации о {PersonId} на {Url}", personContact.UniversityIdContact, url);
                        continue;
                    }

                    // Пример: обновляем должности (itemprop='post')
                    var positionNodes = mainNode.SelectNodes(".//div[@itemprop='post']/li");
                    personContact.Position = positionNodes?.Select(n => n.InnerText.Trim()).ToArray();

                    // Пример: предметы (itemprop='teachingDiscipline')
                    var taughtSubjectsNodes = mainNode.SelectNodes(".//div[@itemprop='teachingDiscipline']/li");
                    var taughtSubjects = taughtSubjectsNodes?.Select(n => n.InnerText.Trim()).ToList();
                    if (taughtSubjects != null)
                    {
                        foreach (var subjectName in taughtSubjects)
                        {
                            await AddOrUpdateSubject(context, personContact, subjectName);
                        }
                    }

                    // Пример: академическая степень (itemprop='degree')
                    var academicDegreeNode = mainNode.SelectSingleNode(".//div[@itemprop='degree']/li");
                    personContact.AcademicDegree = academicDegreeNode?.InnerText?.Trim();

                    // Пример: опыт преподавания (itemprop='specExperience')
                    var teachingExperienceNode = mainNode.SelectSingleNode(".//div[@itemprop='specExperience']/li");
                    personContact.TeachingExperience = teachingExperienceNode?.InnerText?.Trim();

                    await context.SaveChangesAsync();
                }
            }
        }

        /// <summary>
        /// Добавляем или обновляем предмет, связанный с преподавателем (PersonTaughtSubjectData)
        /// </summary>
        private async Task AddOrUpdateSubject(ApplicationDbContext context, PersonContactData personContact, string subjectName)
        {
            // 1) Проверяем, есть ли такой предмет
            var existingSubject = await context.Subjects.FirstOrDefaultAsync(s => s.SubjectName == subjectName);
            if (existingSubject == null)
            {
                // Если нет - добавляем
                var subject = new SubjectData { SubjectName = subjectName };
                context.Subjects.Add(subject);
                await context.SaveChangesAsync();
            }

            // 2) Проверяем PersonTaughtSubjects
            var existingPersonTaughtSubject = await context.PersonTaughtSubjects
                .FirstOrDefaultAsync(pts => pts.IdContact == personContact.IdContact && pts.SubjectName == subjectName);

            if (existingPersonTaughtSubject == null)
            {
                var personTaughtSubject = new PersonTaughtSubjectData
                {
                    IdContact = personContact.IdContact,
                    SubjectName = subjectName
                };
                context.PersonTaughtSubjects.Add(personTaughtSubject);
                await context.SaveChangesAsync();
            }
        }

        public void Dispose()
        {
            _cts?.Cancel();
            _httpClient?.Dispose();
        }
    }
}
