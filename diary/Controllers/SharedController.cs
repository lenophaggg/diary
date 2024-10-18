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
using diary.Enums;

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
        [Route("{role}/{action}")]
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

        [Authorize(Roles = "Admin, GroupHead")]
        [Route("{role}/{action}")]
        [HttpPost]
        public async Task<IActionResult> RemoveStudent(int studentId)
        {
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            var student = await _diaryDbContext.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            using (var transaction = await _diaryDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    // Delete StudentAbsencesData
                    var studentAbsences = await _diaryDbContext.StudentAbsences
                        .Where(sa => sa.StudentId == studentId)
                        .ToListAsync();
                    _diaryDbContext.StudentAbsences.RemoveRange(studentAbsences);

                    // Delete AttendanceData
                    var attendanceEntries = await _diaryDbContext.Attendance
                        .Where(a => a.StudentId == studentId)
                        .ToListAsync();
                    _diaryDbContext.Attendance.RemoveRange(attendanceEntries);

                    // Check if the student is a GroupHead
                    var groupHead = await _diaryDbContext.GroupHeads
                        .FirstOrDefaultAsync(gh => gh.StudentId == studentId);
                    if (groupHead != null)
                    {
                        var userId = groupHead.UserId;

                        // Remove GroupHeadData entry
                        _diaryDbContext.GroupHeads.Remove(groupHead);

                        // Find and remove PersonContactUserData
                        var personContactUser = await _diaryDbContext.PersonContactUsers
                            .FirstOrDefaultAsync(pcu => pcu.UserId == userId);
                        if (personContactUser != null)
                        {
                            _diaryDbContext.PersonContactUsers.Remove(personContactUser);
                        }

                        // Delete the IdentityUser
                        var user = await _userManager.FindByIdAsync(userId);
                        if (user != null)
                        {
                            var result = await _userManager.DeleteAsync(user);
                            if (!result.Succeeded)
                            {
                                throw new Exception("Failed to delete associated user.");
                            }
                        }
                    }

                    // Remove the Student
                    _diaryDbContext.Students.Remove(student);

                    // Save changes
                    await _diaryDbContext.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Json(new { success = true });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Ошибка при удалении студента." });
                }
            }
        }

        // Метод создания заявки на отсутсвие по уважительной причине
        [Authorize(Roles = "Admin, GroupHead")]
        [Route("{role}/{action}")]
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
        [Route("{role}/{action}")]
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
        [Route("{role}/{action}")]
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

        [Authorize(Roles = "Admin, GroupHead")]
        [Route("{role}/{action}")]
        public async Task<IActionResult> Classes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            List<ClassViewItem> classViewItems;
            string groupNumber = null;

            if (User.IsInRole("Admin"))
            {
                var classes = await _diaryDbContext.Classes.ToListAsync();

                var instructorIds = classes.Select(c => c.InstructorId).Distinct().ToList();

                var instructors = await _applicationDbContext.PersonContacts
                    .Where(pc => instructorIds.Contains(pc.IdContact))
                    .ToDictionaryAsync(pc => pc.IdContact, pc => pc.NameContact);

                classViewItems = classes.Select(c => new ClassViewItem
                {
                    ClassId = c.ClassId,
                    Subject = c.Subject,
                    Semester = c.Semester,
                    AcademicYear = c.AcademicYear,
                    Type = c.Type,
                    GroupNumber = c.GroupNumber,
                    InstructorName = instructors.ContainsKey(c.InstructorId) ? instructors[c.InstructorId] : "Неизвестно"
                }).ToList();

                ViewBag.UserRole = "Admin";
            }
            else if (User.IsInRole("GroupHead"))
            {
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null)
                {
                    return NotFound("Староста не найден");
                }

                groupNumber = groupHead.Student.GroupNumber;

                var classes = await _diaryDbContext.Classes
                    .Where(c => c.GroupNumber == groupNumber)
                    .ToListAsync();

                var instructorIds = classes.Select(c => c.InstructorId).Distinct().ToList();

                var instructors = await _applicationDbContext.PersonContacts
                    .Where(pc => instructorIds.Contains(pc.IdContact))
                    .ToDictionaryAsync(pc => pc.IdContact, pc => pc.NameContact);

                classViewItems = classes.Select(c => new ClassViewItem
                {
                    ClassId = c.ClassId,
                    Subject = c.Subject,
                    Semester = c.Semester,
                    AcademicYear = c.AcademicYear,
                    Type = c.Type,
                    GroupNumber = c.GroupNumber,
                    InstructorName = instructors.ContainsKey(c.InstructorId) ? instructors[c.InstructorId] : "Неизвестно"
                }).ToList();

                ViewBag.UserRole = "GroupHead";
                ViewBag.GroupNumber = groupNumber;
            }
            else
            {
                return BadRequest(new { success = false, message = "User role not found" });
            }

            return View(new ClassViewModel
            {
                Classes = classViewItems
            });
        }

        [Authorize(Roles = "Admin, GroupHead")]
        [HttpGet]
        public async Task<IActionResult> FilterClasses(string groupNumber)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            List<ClassData> classes;

            if (User.IsInRole("Admin"))
            {
                if (string.IsNullOrEmpty(groupNumber))
                {
                    classes = await _diaryDbContext.Classes.ToListAsync();
                }
                else
                {
                    classes = await _diaryDbContext.Classes
                        .Where(c => c.GroupNumber == groupNumber)
                        .ToListAsync();
                }
            }
            else if (User.IsInRole("GroupHead"))
            {
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null)
                {
                    return NotFound("Староста не найден");
                }

                groupNumber = groupHead.Student.GroupNumber;

                classes = await _diaryDbContext.Classes
                    .Where(c => c.GroupNumber == groupNumber)
                    .ToListAsync();
            }
            else
            {
                return Unauthorized();
            }

            var instructorIds = classes.Select(c => c.InstructorId).Distinct().ToList();

            var instructors = await _applicationDbContext.PersonContacts
                .Where(pc => instructorIds.Contains(pc.IdContact))
                .ToDictionaryAsync(pc => pc.IdContact, pc => pc.NameContact);

            var classViewItems = classes.Select(c => new ClassViewItem
            {
                ClassId = c.ClassId,
                Subject = c.Subject,
                Semester = c.Semester,
                AcademicYear = c.AcademicYear,
                Type = c.Type,
                GroupNumber = c.GroupNumber,
                InstructorName = instructors.ContainsKey(c.InstructorId) ? instructors[c.InstructorId] : "Неизвестно"
            }).ToList();

            return PartialView("_ClassesTable", classViewItems);
        }

        [HttpGet]
        public async Task<IActionResult> GetClass(int classId)
        {
            var classData = await _diaryDbContext.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new
                {
                    classId = c.ClassId,
                    subject = c.Subject,
                    semester = c.Semester,
                    academicYear = c.AcademicYear,
                    lessonType = c.Type.ToString(),
                    instructorId = c.InstructorId,
                    groupNumber = c.GroupNumber
                })
                .FirstOrDefaultAsync();

            if (classData == null)
            {
                return NotFound();
            }

            var instructor = await _applicationDbContext.PersonContacts
                .FirstOrDefaultAsync(pc => pc.IdContact == classData.instructorId);

            var instructorName = instructor?.NameContact ?? "";

            return Json(new
            {
                classId = classData.classId,
                subject = classData.subject,
                semester = classData.semester,
                academicYear = classData.academicYear,
                lessonType = classData.lessonType,
                instructorName = instructorName,
                groupNumber = classData.groupNumber
            });
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass(string subjectName, int semester, string academicYear, string lessonType, string instructorName, string groupNumber)
        {
            subjectName = subjectName.Trim();
            academicYear = academicYear.Trim();

            var instructor = await _applicationDbContext.PersonContacts
                .FirstOrDefaultAsync(pc => pc.NameContact == instructorName);

            if (instructor == null)
            {
                return BadRequest(new { success = false, message = "Преподаватель не найден" });
            }

            if (!Enum.TryParse(lessonType, true, out LessonType parsedLessonType))
            {
                return BadRequest(new { success = false, message = "Invalid lesson type" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("GroupHead"))
            {
                // Получаем номер группы старосты
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null)
                {
                    return NotFound("Group head not found");
                }

                groupNumber = groupHead.Student.GroupNumber;
            }

            var existingClass = await _diaryDbContext.Classes
                .FirstOrDefaultAsync(c => c.Subject == subjectName
                    && c.InstructorId == instructor.IdContact
                    && c.Type == parsedLessonType
                    && c.GroupNumber == groupNumber);

            if (existingClass != null)
            {
                return BadRequest(new { success = false, message = "Такое занятие уже существует!" });
            }

            var newClass = new ClassData
            {
                Subject = subjectName,
                InstructorId = instructor.IdContact,
                GroupNumber = groupNumber,
                Semester = semester,
                AcademicYear = academicYear,
                Type = parsedLessonType
            };

            _diaryDbContext.Classes.Add(newClass);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClass(int classId, string subjectName, int semester, string academicYear, string lessonType, string instructorName, string groupNumber)
        {
            var existingClass = await _diaryDbContext.Classes.FindAsync(classId);
            if (existingClass == null)
            {
                return NotFound(new { success = false, message = "Занятие не найдено" });
            }

            existingClass.Subject = subjectName;
            existingClass.Semester = semester;
            existingClass.AcademicYear = academicYear;

            if (!Enum.TryParse(lessonType, true, out LessonType parsedLessonType))
            {
                return BadRequest(new { success = false, message = "Неправильный тип занятия" });
            }

            existingClass.Type = parsedLessonType;

            var instructor = await _applicationDbContext.PersonContacts
                .FirstOrDefaultAsync(pc => pc.NameContact == instructorName);

            if (instructor == null)
            {
                return BadRequest(new { success = false, message = "Преподаватель не найден" });
            }

            existingClass.InstructorId = instructor.IdContact;

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (User.IsInRole("GroupHead"))
            {
                // Староста не может изменять номер группы
                var groupHead = await _diaryDbContext.GroupHeads
                    .Include(gh => gh.Student)
                    .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

                if (groupHead == null)
                {
                    return NotFound("Group head not found");
                }

                existingClass.GroupNumber = groupHead.Student.GroupNumber;
            }
            else if (User.IsInRole("Admin"))
            {
                // Администратор может изменять номер группы
                existingClass.GroupNumber = groupNumber;
            }

            _diaryDbContext.Classes.Update(existingClass);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteClass(int classId)
        {
            var classEntity = await _diaryDbContext.Classes.FindAsync(classId);
            if (classEntity == null)
            {
                return Json(new { success = false, message = "Занятие не найдено." });
            }

            var attendanceRecords = await _diaryDbContext.Attendance
                .Where(a => a.ClassId == classEntity.ClassId)
                .ToListAsync();

            if (attendanceRecords.Any())
            {
                _diaryDbContext.Attendance.RemoveRange(attendanceRecords);
            }

            _diaryDbContext.Classes.Remove(classEntity);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetInstructors()
        {
            try
            {
                var instructors = await _applicationDbContext.PersonContacts
                    .Select(pc => new
                    {
                        idContact = pc.IdContact,
                        nameContact = pc.NameContact
                    })
                    .ToListAsync();

                return Json(instructors);
            }
            catch
            {
                return StatusCode(500, new { success = false, message = "Ошибка при получении списка преподавателей" });
            }
        }

        [Route("{role}/{action}")]
        public async Task<IActionResult> ManageAttendance(int classId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            if (classId == 0)
            {
                return RedirectToAction("Index", User.IsInRole("Teacher") ? "Teacher" : "GroupHead");
            }

            var classData = await _diaryDbContext.Classes
                .FirstOrDefaultAsync(a => a.ClassId == classId);

            if (classData == null)
            {
                return NotFound("Class not found");
            }

            var students = await _diaryDbContext.Students
                .Where(s => s.GroupNumber == classData.GroupNumber)
                .ToListAsync();

            var attendanceRecords = await _diaryDbContext.Attendance
                .Where(a => a.ClassId == classData.ClassId)
                .ToListAsync();

            var orderedAttendanceRecords = attendanceRecords
                .OrderBy(a => a.Date)
                .ThenBy(a => a.SessionNumber)
                .ToList();

            // Вычисляем TotalClasses на основе уникальных сессий (дата + номер сессии)
            int totalClasses = attendanceRecords
                .Select(a => new { a.Date, a.SessionNumber })
                .Distinct()
                .Count();

            var viewModel = new ManageAttendanceViewModel
            {
                ClassId = classId,
                SubjectName = classData.Subject,
                SubjectType = classData.Type.ToString(),
                Students = students,
                GroupNumber = classData.GroupNumber,
                AttendanceRecords = orderedAttendanceRecords,
                TotalClasses = totalClasses // Устанавливаем TotalClasses здесь
            };

            return View("ManageAttendance", viewModel);
        }

        [HttpPost]
        [Authorize(Roles = "Admin, GroupHead")]
        public async Task<IActionResult> SaveAttendance([FromBody] List<AttendanceData> attendanceData)
        {
            if (attendanceData == null || !attendanceData.Any())
            {
                return BadRequest("Данные посещаемости не были предоставлены.");
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            using (var transaction = await _diaryDbContext.Database.BeginTransactionAsync())
            {
                try
                {
                    foreach (var record in attendanceData)
                    {
                        var classData = await _diaryDbContext.Classes
                            .AsNoTracking()
                            .FirstOrDefaultAsync(c => c.ClassId == record.ClassId);

                        if (classData == null)
                        {
                            _logger.LogWarning($"Занятие с ID {record.ClassId} не найдено.");
                            continue;
                        }

                        var groupNumber = classData.GroupNumber;
                        var recordDateAsDateTime = record.Date.ToDateTime(TimeOnly.MinValue);

                        bool isExcusedAbsence = await _diaryDbContext.StudentAbsences
                            .AnyAsync(sa => sa.StudentId == record.StudentId
                                        && sa.GroupNumber == groupNumber
                                        && sa.Status == AbsencesStatus.Approved
                                        && sa.StartDate <= recordDateAsDateTime
                                        && sa.EndDate >= recordDateAsDateTime);

                        bool isPresent = record.IsPresent;
                        if (isExcusedAbsence && isPresent)
                        {
                            isPresent = false;
                        }

                        var existingRecord = await _diaryDbContext.Attendance
                            .FirstOrDefaultAsync(a => a.ClassId == record.ClassId
                                                   && a.StudentId == record.StudentId
                                                   && a.Date == record.Date
                                                   && a.SessionNumber == record.SessionNumber);

                        if (existingRecord != null)
                        {
                            existingRecord.IsPresent = isPresent;
                            existingRecord.IsExcusedAbsence = isExcusedAbsence;

                            _diaryDbContext.Attendance.Update(existingRecord);
                        }
                        else
                        {
                            var newRecord = new AttendanceData
                            {
                                ClassId = record.ClassId,
                                StudentId = record.StudentId,
                                Date = record.Date,
                                SessionNumber = record.SessionNumber,
                                IsPresent = isPresent,
                                IsExcusedAbsence = isExcusedAbsence
                            };
                            await _diaryDbContext.Attendance.AddAsync(newRecord);
                        }
                    }

                    await _diaryDbContext.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Ok(new { success = true });
                }
                catch (System.Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Ошибка при сохранении посещаемости");
                    return StatusCode(500, "Произошла ошибка при сохранении посещаемости.");
                }
            }
        }

        [Authorize(Roles = "Teacher, Admin")]
        [HttpGet]
        public async Task<IActionResult> ExportToExcel(int classId)
        {
            var classData = await _diaryDbContext.Classes
                .FirstOrDefaultAsync(c => c.ClassId == classId);

            if (classData == null)
            {
                return NotFound("Class not found");
            }

            var instructorName = await _applicationDbContext.PersonContacts
                 .Where(pc => pc.IdContact == classData.InstructorId)
                 .Select(pc => pc.NameContact)
                 .FirstOrDefaultAsync();

            var attendanceData = await _diaryDbContext.Attendance
               .Where(a => a.ClassId == classId)
               .Include(a => a.Student)
               .Include(a => a.Class)
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

            var translatedType = lessonTypes.ContainsKey(classData.Type.ToString().ToLower())
                ? lessonTypes[classData.Type.ToString().ToLower()]
                : classData.Type.ToString();

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Attendance");

                // Заголовок таблицы
                worksheet.Cells[1, 1].Value = $"Посещаемость группы {classData.GroupNumber} по предмету \"{classData.Subject}\" ({translatedType}) преподавателя {instructorName}";
                worksheet.Cells[1, 1, 1, uniqueDatesSessions.Count + 1].Merge = true;
                worksheet.Cells[1, 1].Style.Font.Bold = true;
                worksheet.Cells[1, 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                worksheet.Cells[1, 1].Style.Font.Size = 14;

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
                            else if (attendanceRecord.IsExcusedAbsence)
                            {
                                // Уважительная причина
                                cell.Value = "Отсутствие (уваж.)";
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightYellow);
                            }
                            else
                            {
                                // Неуважительное отсутствие
                                cell.Value = "Отсутствует";
                                cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                                cell.Style.Fill.BackgroundColor.SetColor(Color.LightCoral);
                            }
                        }
                        else
                        {
                            cell.Value = "Нет данных";
                            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            cell.Style.Fill.BackgroundColor.SetColor(Color.LightGray);
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
                worksheet.Column(1).Width = 35;
                for (int i = 2; i <= uniqueDatesSessions.Count + 1; i++)
                {
                    worksheet.Column(i).Width = 20;
                }

                var fileName = $"{classData.GroupNumber}_{classData.Subject.Replace(' ', '_')}_{translatedType.Replace(' ', '_')}_{instructorName.Replace(' ', '_')}.xlsx";
                var excelBytes = package.GetAsByteArray();
                return File(excelBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
        }

    }
}
