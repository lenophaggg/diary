using diary.Data;
using diary.Models;
using diary.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace diary.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly DiaryDbContext _diaryDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public AdminController(ILogger<AdminController> logger,
            ApplicationDbContext applicationDbContext,
            DiaryDbContext diaryDbContext,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _logger = logger;
            _applicationDbContext = applicationDbContext;
            _diaryDbContext = diaryDbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var personContactUser = _diaryDbContext.PersonContactUsers
                                            .FirstOrDefault(pcu => pcu.UserId == user.Id);
            if (personContactUser == null)
            {
                return View();
            }

            var personContact = _applicationDbContext.PersonContacts
                .FirstOrDefault(pc => pc.IdContact == personContactUser.PersonContactId);

            return View(personContact);
        }

        public async Task<IActionResult> Classes()
        {
            return RedirectToAction("Classes", "Shared");
        }

        public async Task<IActionResult> TeacherDetails(int id)
        {
            if (id == 0)
            {
                return RedirectToAction("Index", "Teacher"); // Перенаправление на главную страницу или другую страницу по вашему выбору
            }
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            var teacher = await _applicationDbContext.PersonContacts
               .AsNoTracking()
               .FirstOrDefaultAsync(t => t.IdContact == id);

            if (teacher == null)
            {
                return NotFound();
            }

            
            var personContactUser = await _diaryDbContext.PersonContactUsers
                .AsNoTracking()
                .FirstOrDefaultAsync(pcu => pcu.PersonContactId == id);

            IdentityUser user = null;
            List<string> userRoles = new List<string>();

            if (personContactUser != null)
            {
                user = await _userManager.FindByIdAsync(personContactUser.UserId);
                if (user != null)
                {
                    userRoles = (await _userManager.GetRolesAsync(user)).ToList();
                }
            }

            var model = new
            {
                teacher.IdContact,
                teacher.UniversityIdContact,
                teacher.NameContact,
                teacher.Position,
                teacher.AcademicDegree,
                teacher.TeachingExperience,
                teacher.Telephone,
                teacher.Email,
                teacher.Information,
                teacher.ImgPath,
                UserId = user?.Id,
                UserName = user?.UserName,
                UserRoles = userRoles,
                Roles = await _roleManager.Roles.ToListAsync(),
                Classes = await _diaryDbContext.Classes              
                                  .Where(c => c.InstructorId == teacher.IdContact)
                                  .ToListAsync() 
            };
            return View(model);
        }

        // Метод для отображения списка преподавателей
        [HttpGet]
        public async Task<IActionResult> ListTeacher()
        {
            try
            {
                var teachers = await _applicationDbContext.PersonContacts.ToListAsync();
                return View(teachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при загрузке списка преподавателей.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpGet]
        public async Task<IActionResult> FilterTeachers(string searchTerm)
        {
            try
            {
                var query = _applicationDbContext.PersonContacts.AsQueryable();

                if (!string.IsNullOrEmpty(searchTerm))
                {
                    string lowerSearchTerm = searchTerm.ToLower();
                    query = query.Where(t => t.NameContact.ToLower().Contains(lowerSearchTerm));
                }

                var filteredTeachers = await query.ToListAsync();
                return PartialView("_TeachersTable", filteredTeachers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при фильтрации преподавателей.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTeacher(string teacherName, string teacherUniversityId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(teacherName) || string.IsNullOrWhiteSpace(teacherUniversityId))
                {
                    return Json(new { success = false, message = "Заполните все поля." });
                }

                if (!teacherUniversityId.All(char.IsDigit))
                {
                    return Json(new { success = false, message = "ISU ID должен состоять только из цифр." });
                }

                var existingTeacher = await _applicationDbContext.PersonContacts
                    .FirstOrDefaultAsync(t => t.UniversityIdContact == teacherUniversityId);
                if (existingTeacher != null)
                {
                    return Json(new { success = false, message = "Преподаватель с таким ISU ID уже существует." });
                }

                string bigImageUrl = $"https://isu.smtu.ru/images/isu_person/big/p{teacherUniversityId}.jpg";
                string smallImageUrl = $"https://isu.smtu.ru/images/isu_person/small/p{teacherUniversityId}.jpg";
                string imgPath = smallImageUrl;

                try
                {
                    using (HttpClient httpClient = new HttpClient())
                    {
                        HttpResponseMessage response = await httpClient.GetAsync(bigImageUrl);
                        if (response.IsSuccessStatusCode)
                        {
                            imgPath = bigImageUrl;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Не удалось загрузить большое изображение, используется маленькое.");
                    imgPath = smallImageUrl;
                }

                var newTeacher = new PersonContactData
                {
                    NameContact = teacherName.Trim(),
                    UniversityIdContact = teacherUniversityId.Trim(),
                    ImgPath = imgPath
                };

                await _applicationDbContext.PersonContacts.AddAsync(newTeacher);
                int changes = await _applicationDbContext.SaveChangesAsync();

                if (changes > 0)
                {
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, message = "Не удалось сохранить данные." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении преподавателя.");
                return StatusCode(500, "Внутренняя ошибка сервера.");
            }
        }

        #region ManageUsers

        [HttpPost]
        public async Task<IActionResult> CreateUser(int personContactId, string userName, string password, string[] userRoles, string contactType)
        {
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(userName))
            {
                return Json(new { success = false, message = "User name cannot be empty" });
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                return Json(new { success = false, message = "Password cannot be empty" });
            }

            if (userRoles == null || userRoles.Length == 0)
            {
                return Json(new { success = false, message = "At least one role must be selected" });
            }

            if (string.IsNullOrWhiteSpace(contactType))
            {
                return Json(new { success = false, message = "Contact type cannot be empty" });
            }

            object personContact = null;

            if (contactType == "Teacher")
            {
                personContact = await _applicationDbContext.PersonContacts.FirstOrDefaultAsync(pc => pc.IdContact == personContactId);
            }
            else if (contactType == "GroupHead")
            {
                personContact = await _diaryDbContext.GroupHeads.Include(gh => gh.Student).FirstOrDefaultAsync(gh => gh.Student.StudentId == personContactId);
            }

            if (personContact == null)
            {
                return Json(new { success = false, message = $"{contactType} not found" });
            }


            var user = new IdentityUser { UserName = userName.Trim() };
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                if (contactType == "Teacher")
                {
                    var personContactUser = new PersonContactUserData
                    {
                        UserId = user.Id,
                        PersonContactId = personContactId
                    };
                    _diaryDbContext.PersonContactUsers.Add(personContactUser);
                }
                else if (contactType == "GroupHead")
                {
                    var groupHead = _diaryDbContext.GroupHeads.FirstOrDefault(gh => gh.StudentId == personContactId);
                    groupHead.UserId = user.Id;
                    _diaryDbContext.GroupHeads.Update(groupHead);
                }

                await _diaryDbContext.SaveChangesAsync();

                foreach (var role in userRoles)
                {
                    if (!await _roleManager.RoleExistsAsync(role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(role));
                    }
                    await _userManager.AddToRoleAsync(user, role);
                }

                return Json(new { success = true });
            }
            else
            {
                return Json(new { success = false, message = string.Join(", ", result.Errors.Select(e => e.Description)) });
            }
        }

        [HttpPost]
        public async Task<IActionResult> DeleteUser(int personContactId, string contactType)
        {
            // Получаем текущего пользователя
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            object personContactUser = null;
            string userId = null;

            // Определяем, с каким типом пользователя работаем (Teacher или GroupHead)
            if (contactType == "Teacher")
            {
                var teacherContact = await _diaryDbContext.PersonContactUsers
                    .FirstOrDefaultAsync(pcu => pcu.PersonContactId == personContactId);
                if (teacherContact != null)
                {
                    personContactUser = teacherContact;
                    userId = teacherContact.UserId;
                }
            }
            else if (contactType == "GroupHead")
            {
                var groupHeadContact = await _diaryDbContext.GroupHeads
                    .FirstOrDefaultAsync(gh => gh.StudentId == personContactId);
                if (groupHeadContact != null)
                {
                    personContactUser = groupHeadContact;
                    userId = groupHeadContact.UserId;
                }
            }

            // Если связь с пользователем не найдена
            if (personContactUser == null)
            {
                return Json(new { success = false, message = "User assignment not found" });
            }

            // Если UserId существует, удаляем связанного пользователя в системе Identity
            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    var result = await _userManager.DeleteAsync(user);
                    if (!result.Succeeded)
                    {
                        return Json(new { success = false, message = "Failed to delete the user" });
                    }
                }
            }

            // Обработка в зависимости от типа
            if (contactType == "Teacher")
            {
                // Удаление записи из таблицы person_contact_users
                _diaryDbContext.PersonContactUsers.Remove((PersonContactUserData)personContactUser);
            }
            else if (contactType == "GroupHead")
            {
                // Установка поля UserId в null, вместо удаления записи
                var groupHead = (GroupHeadData)personContactUser;
                groupHead.UserId = null;
                _diaryDbContext.GroupHeads.Update(groupHead);
            }

            // Сохранение изменений в базе данных
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }



        [HttpGet]
        public async Task<IActionResult> GetUserRoles(string userId)
        {

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Json(roles);
        }


        [HttpPost]
        public async Task<IActionResult> UpdateUserRoles(string userId, List<string> roles)
        {
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "User ID is required" });
            }

            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Json(new { success = false, message = "User not found" });
            }

            var currentRoles = await _userManager.GetRolesAsync(user);
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
            if (!removeRolesResult.Succeeded)
            {
                return Json(new { success = false, message = "Failed to remove existing roles" });
            }

            var addRolesResult = await _userManager.AddToRolesAsync(user, roles);
            if (!addRolesResult.Succeeded)
            {
                return Json(new { success = false, message = "Failed to add new roles" });
            }

            return Json(new { success = true });
        }

        // Вспомогательный метод для проверки роли
        public async Task<bool> IsUserInRole(string userId, string roleName)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user != null)
            {
                return await _userManager.IsInRoleAsync(user, roleName);
            }
            return false;
        }
        #endregion
        #region ManageGroup
        public async Task<IActionResult> ListGroup()
        {
            var groups = _applicationDbContext.Groups.ToList();

            var facultyGroups = groups
                .Where(g => g.FacultyName != null) // Добавляем проверку на null
                .OrderBy(g => g.Number) // Сортируем группы по номеру
                .GroupBy(g => g.FacultyName)
                .OrderByDescending(g => g.Key) // Сортируем факультеты в обратном порядке
                .ToDictionary(g => g.Key, g => g.Select(group => group.Number).ToArray());

            return View(facultyGroups);
        }

        public async Task<IActionResult> GroupDetails(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return RedirectToAction("Index", "Admin");
            }

            string group = id;

            // Получаем список студентов группы
            var students = await _diaryDbContext.Students
                .Where(s => s.GroupNumber == group)
                .ToListAsync();

            // Получаем старосту группы
            var grouphead = await _diaryDbContext.GroupHeads
                .Include(gh => gh.Student)
                .Where(gh => gh.Student.GroupNumber == group)
                .Select(gh => gh.Student)
                .FirstOrDefaultAsync();

            // Шаг 1: Получаем общее количество занятий по каждому предмету для группы
            var totalClassesPerSubject = await _diaryDbContext.Attendance
                .Where(a => a.Student.GroupNumber == group)
                .Select(a => new { a.Class.Subject, a.Date, a.SessionNumber })
                .Distinct()
                .ToListAsync();

            // Группируем по предмету и считаем количество занятий
            var totalClassesDict = totalClassesPerSubject
                .GroupBy(tc => tc.Subject)
                .ToDictionary(g => g.Key, g => g.Count());

            // Шаг 2: Получаем данные посещаемости для всех студентов группы
            var attendanceData = await _diaryDbContext.Attendance
                .Where(a => a.Student.GroupNumber == group)
                .Select(a => new
                {
                    a.Student.StudentId,
                    a.Student.Name,
                    a.Class.Subject,
                    a.IsPresent,
                    a.IsExcusedAbsence,
                    a.Date,
                    a.SessionNumber
                })
                .ToListAsync();

            // Шаг 3: Группируем данные по студенту и предмету и рассчитываем посещаемость
            var attendanceReports = attendanceData
                .GroupBy(a => new { a.StudentId, a.Name, a.Subject })
                .Select(g => new
                {
                    g.Key.StudentId,
                    g.Key.Name,
                    g.Key.Subject,
                    Presents = g
                        .Where(a => a.IsPresent)
                        .Select(a => new { a.Date, a.SessionNumber })
                        .Distinct()
                        .Count(),
                    ExcusedAbsences = g
                        .Where(a => a.IsExcusedAbsence)
                        .Select(a => new { a.Date, a.SessionNumber })
                        .Distinct()
                        .Count()
                })
                .ToList();

            // Шаг 4: Рассчитываем процент посещаемости
            var finalAttendanceReports = attendanceReports.Select(r => new AttendanceReport
            {
                StudentId = r.StudentId,
                StudentName = r.Name,
                SubjectName = r.Subject,
                AttendancePercentage = totalClassesDict.ContainsKey(r.Subject) && (totalClassesDict[r.Subject] - r.ExcusedAbsences) > 0
                    ? ((double)r.Presents / (totalClassesDict[r.Subject] - r.ExcusedAbsences)) * 100.0
                    : 0.0
            }).ToList();

            var groupDetailsViewModel = new GroupDetailsViewModel
            {
                GroupNumber = group,
                Students = students,
                GroupHead = grouphead,
                AttendanceReports = finalAttendanceReports
            };

            return View(groupDetailsViewModel);
        }
        #endregion

        #region GroupHead
        public async Task<IActionResult> GroupHeadDetails(int id)
        {
            if (id == 0)
            {
                return RedirectToAction("Index", "Admin"); // Перенаправление на главную страницу или другую страницу по вашему выбору
            }

            var studentId = id;

            var groupHead = await _diaryDbContext.GroupHeads
                .Include(gh => gh.Student)
                .FirstOrDefaultAsync(gh => gh.StudentId == studentId);

            if (groupHead == null)
            {
                return NotFound("Group head not found");
            }

            var user = await _userManager.FindByIdAsync(groupHead.UserId);
            var userRoles = user != null ? await _userManager.GetRolesAsync(user) : new List<string>();

            var model = new
            {
                groupHead.Student.StudentId,
                groupHead.Student.Name,
                groupHead.Student.UniversityStudentId,
                groupHead.Student.GroupNumber,
                groupHead.UserId,
                UserName = user?.UserName,
                UserRoles = userRoles,
                Roles = await _roleManager.Roles.ToListAsync()
            };

            return View(model);
        }


        [HttpPost]
        public async Task<IActionResult> AssignGroupHead(int studentId)
        {
            var student = await _diaryDbContext.Students.FindAsync(studentId);
            if (student == null)
            {
                return Json(new { success = false, message = "Student not found" });
            }

            // Удаление текущего старосты
            var currentGroupHead = await _diaryDbContext.GroupHeads
                .Include(gh => gh.Student)
                .FirstOrDefaultAsync(gh => gh.Student.GroupNumber == student.GroupNumber);

            if (currentGroupHead != null)
            {
                var userId = currentGroupHead.UserId;
                _diaryDbContext.GroupHeads.Remove(currentGroupHead);

                if (!string.IsNullOrEmpty(userId))
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        await _userManager.DeleteAsync(user);

                    }
                }
            }

            // Назначение нового старосты группы
            var newGroupHead = new GroupHeadData
            {
                StudentId = studentId,
                UserId = null // Пользователь на данном этапе не назначается
            };

            _diaryDbContext.GroupHeads.Add(newGroupHead);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveGroupHead(int studentId)
        {
            var groupHead = await _diaryDbContext.GroupHeads.FirstOrDefaultAsync(gh => gh.StudentId == studentId);
            if (groupHead == null)
            {
                return Json(new { success = false, message = "Group head not found" });
            }

            var userId = groupHead.UserId;
            _diaryDbContext.GroupHeads.Remove(groupHead);

            if (!string.IsNullOrEmpty(userId))
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    await _userManager.DeleteAsync(user);
                }
            }

            await _diaryDbContext.SaveChangesAsync();
            return Json(new { success = true });
        }

        // Метод для добавления студента в группу
        [HttpPost]
        public async Task<IActionResult> AddStudent(string studentName, string universityStudentId, string groupNumber)
        {
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            var student = await _diaryDbContext.Students.FirstOrDefaultAsync(s => s.UniversityStudentId == universityStudentId);
            if (student != null)
            {
                return BadRequest("Студент уже числится в системе");
            }

            var newStudent = new StudentData
            {
                Name = studentName.Trim(),
                UniversityStudentId = universityStudentId.Trim(),
                GroupNumber = groupNumber
            };

            _diaryDbContext.Students.Add(newStudent);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Метод для удаления студента из группы
        
        #endregion

        #region StudentAbsence

        public async Task<IActionResult> StudentAbsences(int? requestId = null)
        {
            return RedirectToAction("StudentAbsences", "Shared");
        }

        public async Task<IActionResult> CreateStudentAbsenceRequest()
        {
            return RedirectToAction("CreateStudentAbsenceRequest", "Shared");
        }

        public async Task<IActionResult> StudentAbsencesDetails(int requestId)
        {
           
            return View("~/Views/Shared/StudentAbsencesDetails.cshtml");  // Убедитесь, что указанный путь правильный
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateStudentAbsenceRequest(StudentAbsenceViewModel model)
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null)
        //    {
        //        return Unauthorized("User not found");
        //    }

        //    var groupHead = await _diaryDbContext.GroupHeads
        //        .Include(gh => gh.Student)
        //        .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

        //    if (groupHead == null)
        //    {
        //        return NotFound("Group head not found");
        //    }

        //    var newRequest = new StudentAbsencesData
        //    {
        //        StudentId = groupHead.Student.StudentId,
        //        GroupNumber = groupHead.Student.GroupNumber,
        //        Reason = model.Reason,
        //        StartDate = model.StartDate,
        //        EndDate = model.EndDate,
        //        Status = AbsencesStatus.Submitted // Заявка создается сразу с этим статусом
        //    };

          
        //    _diaryDbContext.StudentAbsences.Add(newRequest);
        //    await _diaryDbContext.SaveChangesAsync();

        //    return RedirectToAction("StudentAbsences");
        //}

        #endregion
    }
}