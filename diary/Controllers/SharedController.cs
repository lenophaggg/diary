using diary.Data;
using diary.Models;
using diary.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System.Drawing;

namespace diary.Controllers
{

    public class SharedController : Controller
    {
        private readonly ILogger<AdminController> _logger;
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly DiaryDbContext _diaryDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public SharedController(ILogger<AdminController> logger,
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

        // Метод для отображения списка заявок
        [Authorize(Roles = "Admin, GroupHead")]
        public async Task<IActionResult> StudentAbsences()
        {
            var requests = await GetAbsenceRequestsAsync();

            return View(requests);
        }

        [Authorize(Roles = "Admin, GroupHead")]
        [HttpGet]
        public async Task<IActionResult> GetSubmittedAbsences(bool showSubmittedOnly)
        {
            var requests = await GetAbsenceRequestsAsync(showSubmittedOnly ? AbsencesStatus.Submitted : (AbsencesStatus?)null);

            return PartialView("_AbsencesTable", requests); // PartialView для обновления таблицы
        }

        // Метод для получения всех заявок (общий для всех методов)
        private async Task<List<StudentAbsencesData>> GetAbsenceRequestsAsync(AbsencesStatus? statusFilter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return new List<StudentAbsencesData>();
            }

            var query = _diaryDbContext.StudentAbsences
                .Include(sa => sa.Student)
                .AsQueryable();

            if (User.IsInRole("GroupHead"))
            {
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead != null)
                {
                    query = query.Where(r => r.GroupNumber == groupHead.Student.GroupNumber);
                }
            }

            if (statusFilter.HasValue)
            {
                query = query.Where(r => r.Status == statusFilter);
            }

            return await query.ToListAsync();
        }

        // Метод создания заявки на отсутсвие по уважительной причине
        [Authorize(Roles = "Admin, GroupHead")]
        public async Task<IActionResult> CreateStudentAbsenceRequest()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var model = new StudentAbsenceViewModel
            {
                Students = new List<StudentData>(),
                GroupsNumbers = new List<string>(),
                AbsenceRequest = new StudentAbsenceRequestViewModel
                {
                    StartDate = DateTime.Now,
                    EndDate = DateTime.Now
                },
                 CanEdit = true
            };

            if (User.IsInRole("GroupHead"))
            {
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null) return NotFound("Group head not found");

                model.Students = await _diaryDbContext.Students
                    .Where(s => s.GroupNumber == groupHead.Student.GroupNumber)
                    .ToListAsync();

                model.GroupsNumbers.Add(groupHead.Student.GroupNumber);
            }
            else
            {
                model.Students = await _diaryDbContext.Students.ToListAsync();
                model.GroupsNumbers = await _applicationDbContext.Groups
                    .Select(g => g.Number).OrderBy(number => number).ToListAsync();
            }

            return View("StudentAbsencesDetails", model);
        }

        // Метод для просмотра существующей заявки со статусом минимум "отправлено"
        [Authorize(Roles = "Admin, GroupHead")]
        public async Task<IActionResult> StudentAbsencesDetails(int requestId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound("User not found");

            var request = await _diaryDbContext.StudentAbsences
                .Include(r => r.Student)
                .FirstOrDefaultAsync(r => r.RequestId == requestId);

            if (request == null) return NotFound("Request not found");

            var model = new StudentAbsenceViewModel
            {
                AbsenceRequest = new StudentAbsenceRequestViewModel
                {
                    StudentName = request.Student.Name,
                    RequestId = request.RequestId,
                    StudentId = request.StudentId,
                    GroupNumber = request.GroupNumber,
                    Reason = request.Reason,
                    StartDate = request.StartDate,
                    EndDate = request.EndDate,
                    Status = request.Status
                },
                Students = await _diaryDbContext.Students.ToListAsync(),
                GroupsNumbers = await _applicationDbContext.Groups
                    .Select(g => g.Number).OrderBy(number => number).ToListAsync()
            };

            model.CanEdit = !User.IsInRole("GroupHead");

            return View("StudentAbsencesDetails", model);
        }

        // Метод для редактирования, обновления и первого создания существующей заявки
        [HttpPost]
        [Authorize(Roles = "Admin, GroupHead")]
        public async Task<IActionResult> UpdateStudentAbsenceStatus(StudentAbsenceViewModel model)
        {
            if (!ModelState.IsValid)
            {
                model.CanEdit = true;
                return View("StudentAbsencesDetails", model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                ModelState.AddModelError("", "Пользователь не найден.");
                model.CanEdit = true;
                return View("StudentAbsencesDetails", model);
            }

            // Проверка, что EndDate больше StartDate
            if (model.AbsenceRequest.EndDate < model.AbsenceRequest.StartDate)
            {
                model.CanEdit = true;
                ModelState.AddModelError("EndDate", "Дата окончания должна быть позже даты начала.");
                return View("StudentAbsencesDetails", model);
            }

            var groupExists = await _applicationDbContext.Groups
                .AnyAsync(g => g.Number == model.AbsenceRequest.GroupNumber);

            if (!groupExists)
            {
                model.CanEdit = true;
                ModelState.AddModelError("GroupNumber", "Указанная группа не существует.");
                return View("StudentAbsencesDetails", model);
            }

            var student = await _diaryDbContext.Students
                .FirstOrDefaultAsync(s => s.StudentId == model.AbsenceRequest.StudentId);

            if (student == null)
            {
                model.CanEdit = true;
                ModelState.AddModelError("StudentId", "Студент не найден.");
                return View("StudentAbsencesDetails", model);
            }

            if (student.GroupNumber != model.AbsenceRequest.GroupNumber)
            {
                model.CanEdit = true;
                ModelState.AddModelError("StudentId", "Студент не принадлежит к указанной группе.");
                return View("StudentAbsencesDetails", model);
            }

            // Обновление заявки
            if (model.AbsenceRequest.RequestId.HasValue)
            {
                var request = await _diaryDbContext.StudentAbsences
                    .FirstOrDefaultAsync(r => r.RequestId == model.AbsenceRequest.RequestId.Value);

                if (request == null)
                {
                    ModelState.AddModelError("", "Заявка не найдена.");
                    return View("StudentAbsencesDetails", model);
                }

                // Обновляем данные заявки
                request.StudentId = model.AbsenceRequest.StudentId;
                request.GroupNumber = model.AbsenceRequest.GroupNumber;
                request.Reason = model.AbsenceRequest.Reason;
                request.StartDate = model.AbsenceRequest.StartDate;
                request.EndDate = model.AbsenceRequest.EndDate;
                request.Status = model.AbsenceRequest.Status == AbsencesStatus.Approved
                    ? AbsencesStatus.Approved
                    : AbsencesStatus.Rejected;

                await _diaryDbContext.SaveChangesAsync();
            }
            // Первое создание заявки
            else
            {
                var newRequest = new StudentAbsencesData
                {
                    StudentId = model.AbsenceRequest.StudentId,
                    GroupNumber = model.AbsenceRequest.GroupNumber,
                    Reason = model.AbsenceRequest.Reason,
                    StartDate = model.AbsenceRequest.StartDate,
                    EndDate = model.AbsenceRequest.EndDate,
                    Status = AbsencesStatus.Submitted
                };

                _diaryDbContext.StudentAbsences.Add(newRequest);
                await _diaryDbContext.SaveChangesAsync();
            }

            return RedirectToAction("StudentAbsences");
        }

        public async Task<IActionResult> Classes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Проверка на роль преподавателя
            if (User.IsInRole("Teacher"))
            {
                var instructorId = await _diaryDbContext.PersonContactUsers
                    .Where(pcu => pcu.UserId == user.Id)
                    .Select(pcu => pcu.PersonContactId)
                    .FirstOrDefaultAsync();

                if (instructorId == 0)
                {
                    return BadRequest(new { success = false, message = "Instructor not found" });
                }

                var ClassViewItems = await _diaryDbContext.ClassGroupAssignments
                    .Where(cga => cga.Class.InstructorId == instructorId)
                    .Include(cga => cga.Class)
                    .ToListAsync();

                var instructor = await _applicationDbContext.PersonContacts
                    .Where(pc => pc.IdContact == instructorId)
                    .Select(pc => new { pc.NameContact })
                    .FirstOrDefaultAsync();

                ViewBag.UserRole = "Instructor";
                ViewBag.InstructorName = $"{instructor.NameContact}";

                return View("~/Views/Shared/Classes.cshtml",
                    new ClassViewModel()
                    {
                        ClassGroupAssignments = ClassViewItems,
                        GroupsNumbers = await _applicationDbContext.Groups.Select(g => g.Number).ToListAsync()
                    });
            }

            if (User.IsInRole("Admin"))
            {                
                var classViewItems = await _diaryDbContext.ClassGroupAssignments
                    .Include(cga => cga.Class)
                    .ToListAsync();

                ViewBag.UserRole = "Admin";

                return View("~/Views/Shared/Classes.cshtml",
                    new ClassViewModel()
                    {
                        ClassGroupAssignments = classViewItems
                    });
            }
            
            if (User.IsInRole("GroupHead"))
            {
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null)
                {
                    return NotFound("Group head not found");
                }

                var groupNumber = groupHead.Student.GroupNumber;

                var ClassViewItems = await _diaryDbContext.ClassGroupAssignments
                    .Where(cga => cga.GroupNumber == groupNumber)
                    .Include(cga => cga.Class)
                    .ToListAsync();

                ViewBag.UserRole = "GroupHead";
                ViewBag.GroupNumber = groupNumber;

                return View("~/Views/Shared/Classes.cshtml"
                    , new ClassViewModel()
                    {
                        ClassGroupAssignments = ClassViewItems

                    });
            }

            return BadRequest(new { success = false, message = "User role not found" });
        }

        [Authorize(Roles = "Teacher, GroupHead, Admin")]
        // Открытие представления с управлением посещаемостью
        public async Task<IActionResult> ManageAttendance(int classGroupId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (classGroupId == 0)
            {
                return RedirectToAction(User.IsInRole("Teacher") ? "Index" : "Index", User.IsInRole("Teacher") ? "Teacher" : "GroupHead");
            }


            var classData = await _diaryDbContext.ClassGroupAssignments
                .Include(cga => cga.Class)
                .FirstOrDefaultAsync(a => a.ClassGroupId == classGroupId);

            if (classData == null)
            {
                return NotFound("Class not found");
            }
                        
            var students = await _diaryDbContext.Students
                .Where(s => s.GroupNumber == classData.GroupNumber)
                .ToListAsync();

            var attendanceRecords = await _diaryDbContext.Attendance
                .Where(a => a.ClassGroupId == classData.ClassGroupId)
                .ToListAsync();

            var orderedAttendanceRecords = attendanceRecords
                .OrderBy(a => a.Date)
                .ThenBy(a => a.SessionNumber)
                .ToList();

            var viewModel = new ManageAttendanceViewModel
            {
                ClassGroupId = classGroupId,
                SubjectName = classData.Class.Subject,
                SubjectType = classData.Class.Type.ToString(),
                Students = students,
                GroupNuber = classData.GroupNumber,
                StudyDuration = classData.Class.StudyDuration,
                AttendanceRecords = orderedAttendanceRecords 
            };

            return View(viewModel);
        }

        [Authorize(Roles = "Teacher, Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int classGroupId)
        {
            var classData = await _diaryDbContext.ClassGroupAssignments
                .Include(c => c.Class)
                .FirstOrDefaultAsync(cga => cga.ClassGroupId == classGroupId);

            var instructorName = await _applicationDbContext.PersonContacts
                 .Where(pc => pc.IdContact == classData.Class.InstructorId)
                 .Select(pc => pc.NameContact)
                 .FirstOrDefaultAsync();


            if (classData == null)
            {
                return NotFound("Class not found");
            }

            var attendanceData = await _diaryDbContext.Attendance
               .Where(a => a.ClassGroupId == classGroupId && a.Status == AttendanceStatus.ConfirmedByTeacher)
               .Include(a => a.Student)
               .Include(a => a.ClassGroup)
               .ToListAsync();

            var uniqueDatesSessions = attendanceData
                .GroupBy(a => new { a.Date, a.SessionNumber })
                .OrderBy(g => g.Key.Date)
                .ThenBy(g => g.Key.SessionNumber)
                .Select(g => g.Key)
                .ToList();

            var lessonTypes = new Dictionary<string, string>
    {
        { "laboratoryworks", "Лабораторные работы" },
        { "practicalclasses", "Практические занятия" },
        { "seminars", "Семинары" },
        { "colloquiums", "Коллоквиумы" },
        { "lectures", "Лекции" },
        { "consultations", "Консультации" }
    };

            var translatedType = lessonTypes.ContainsKey(classData.Class.Type.ToString().ToLower())
                ? lessonTypes[classData.Class.Type.ToString().ToLower()]
                : classData.Class.Type.ToString();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Attendance");

                // Заголовок таблицы
                worksheet.Cells[1, 1].Value = $"Посещаемость группы {classData.GroupNumber} по предмету \"{classData.Class.Subject}\" ({translatedType}) преподавателя {instructorName}";
                worksheet.Cells[1, 1, 1, uniqueDatesSessions.Count + 1].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Font.Size = 14; // Увеличим шрифт заголовка

                // Заголовки столбцов
                worksheet.Cells[2, 1].Value = "ФИО студента";
                worksheet.Cells[2, 1].Style.Font.Bold = true;
                worksheet.Cells[2, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                for (int i = 0; i < uniqueDatesSessions.Count; i++)
                {
                    var session = uniqueDatesSessions[i];
                    worksheet.Cells[2, i + 2].Value = $"{session.Date:yyyy-MM-dd} / {session.SessionNumber}";
                    worksheet.Cells[2, i + 2].Style.Font.Bold = true;
                    worksheet.Cells[2, i + 2].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Заполнение данных студентов
                var students = attendanceData.Select(a => a.Student).Distinct().ToList();
                for (int i = 0; i < students.Count; i++)
                {
                    var student = students[i];
                    worksheet.Cells[i + 3, 1].Value = student.Name;
                    worksheet.Cells[i + 3, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;

                    for (int j = 0; j < uniqueDatesSessions.Count; j++)
                    {
                        var session = uniqueDatesSessions[j];
                        var attendanceRecord = attendanceData.FirstOrDefault(a => a.StudentId == student.StudentId && a.Date == session.Date && a.SessionNumber == session.SessionNumber);

                        var cell = worksheet.Cells[i + 3, j + 2];
                        if (attendanceRecord != null)
                        {
                            if (attendanceRecord.IsPresent)
                            {
                                cell.Value = "Присутствует";
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightGreen);
                            }
                            else if (attendanceRecord.IsAbsence)
                            {
                                // Уважительная причина
                                cell.Value = "Отсутствие (уваж.)";
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightYellow); // Цвет для уважительной причины
                            }
                            else
                            {
                                // Неуважительное отсутствие
                                cell.Value = "Отсутствует";
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightCoral); // Цвет для обычного отсутствия
                            }
                        }
                        else
                        {
                            cell.Value = "Отсутствие";
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.LightBlue); // Цвет, если запись отсутствует
                        }
                    }
                }

                // Применим форматирование границ для всей таблицы
                var allCells = worksheet.Cells[1, 1, students.Count + 2, uniqueDatesSessions.Count + 1];
                allCells.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                allCells.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                allCells.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                allCells.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;

                // Добавим автофильтр
                worksheet.Cells[2, 1, 2, uniqueDatesSessions.Count + 1].AutoFilter = true;

                // Настроим ширину столбцов
                worksheet.Column(1).Width = 35; // Ширина для ФИО студента
                for (int i = 2; i <= uniqueDatesSessions.Count + 1; i++)
                {
                    worksheet.Column(i).Width = 20;
                }
                               
                var fileName = $"{classData.GroupNumber}_{classData.Class.Subject.Replace(' ', '_')}_{translatedType.Replace(' ', '_')}_{instructorName.Replace(' ', '_')}.xlsx";
                var excelBytes = package.GetAsByteArray();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }
    }
}
