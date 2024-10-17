using diary.Data;
using diary.Enums;
using diary.Models;
using diary.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using System.Security.Claims;


namespace diary.Controllers
{
    [Authorize(Roles = "Teacher")]
    public class TeacherController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly DiaryDbContext _diaryDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public TeacherController(ApplicationDbContext applicationDbContext, DiaryDbContext diaryDbContext, UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
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
                return NotFound("User not found");
            }

            var personContact = _applicationDbContext.PersonContacts
                .FirstOrDefault(pc => pc.IdContact == personContactUser.PersonContactId);

            return View(personContact);
        }

        // Метод отображения занятий по группам
        public async Task<IActionResult> Classes()
        {
            return RedirectToAction("Classes", "Shared");
        }
        // Метод отображения предметов преподавателя
        public async Task<IActionResult> ListClasses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var instructorId = await _diaryDbContext.PersonContactUsers
                .Where(pcu => pcu.UserId == user.Id)
                .Select(pcu => pcu.PersonContactId)
                .FirstOrDefaultAsync();

            if (instructorId == 0)
            {
                return BadRequest(new { success = false, message = "Instructor not found" });
            }

            var instructorClasses = await _diaryDbContext.Classes
                .Where(c => c.InstructorId == instructorId)
                .ToListAsync();

            var instructor = await _applicationDbContext.PersonContacts
                .Where(pc => pc.IdContact == instructorId)
                .Select(pc => new { pc.NameContact })
                .FirstOrDefaultAsync();

           
            ViewBag.InstructorName = $"{instructor.NameContact}";
            return View(instructorClasses);

           
        }

        //[HttpPost]
        //public async Task<IActionResult> CreateClass(string subjectName, double studyDuration, int semester, string academicYear, string lessonType, string groupNumber)
        //{
        //    subjectName = subjectName.Trim();
        //    groupNumber = groupNumber?.Trim();
        //    academicYear = academicYear.Trim();

        //    var user = await _userManager.GetUserAsync(User);
        //    if (user == null)
        //    {
        //        return Unauthorized();
        //    }

        //    var dbInstructorId = await _diaryDbContext.PersonContactUsers
        //        .Where(pcu => pcu.UserId == user.Id)
        //        .Select(pcu => pcu.PersonContactId)
        //        .FirstOrDefaultAsync();

        //    if (dbInstructorId == 0)
        //    {
        //        return BadRequest(new { success = false, message = "Instructor not found" });
        //    }

        //    if (!Enum.TryParse(lessonType, true, out LessonType parsedLessonType))
        //    {
        //        return BadRequest(new { success = false, message = "Invalid lesson type" });
        //    }

        //    var existingClass = await _diaryDbContext.Classes
        //        .FirstOrDefaultAsync(c => c.Subject == subjectName
        //            && c.GroupNumber == groupNumber
        //            && c.InstructorId == dbInstructorId
        //            && c.Type == parsedLessonType);

        //    if (existingClass != null)
        //    {
        //        return BadRequest(new { success = false, message = "Class with the same subject, group, and lesson type already exists for this instructor." });
        //    }

        //    var newClass = new ClassData
        //    {
        //        Subject = subjectName,
        //        InstructorId = dbInstructorId,
        //        StudyDuration = studyDuration,
        //        Semester = semester,
        //        AcademicYear = academicYear,
        //        Type = parsedLessonType,
        //        GroupNumber = groupNumber
        //    };

        //    _diaryDbContext.Classes.Add(newClass);
        //    await _diaryDbContext.SaveChangesAsync();

        //    return Json(new { success = true });
        //}

        // Удаление предмета 
        [HttpPost]
        public async Task<IActionResult> DeleteClass(int classId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var classEntity = await _diaryDbContext.Classes.FindAsync(classId);
            if (classEntity == null)
            {
                return Json(new { success = false, message = "Занятие не найдено." });
            }
            
            var classGroupAssignments = await _diaryDbContext.Classes
                .Where(cg => cg.ClassId == classEntity.ClassId)
                .ToListAsync(); 
            
            var classGroupAssignmentsClassGroupId = classGroupAssignments.Select(cg => cg.ClassId)
                .ToList(); 

            var attendanceRecords = await _diaryDbContext.Attendance
                .Where(a => classGroupAssignmentsClassGroupId.Contains(a.ClassId)) 
                .ToListAsync();

            // Удаляем все связанные данные о посещаемости
            if (attendanceRecords.Any())
            {
                _diaryDbContext.Attendance.RemoveRange(attendanceRecords);
            }

            // Удаляем все связанные данные о занятиях
            if (classGroupAssignments.Any())
            {
                _diaryDbContext.Classes.RemoveRange(classGroupAssignments);
            }

            // Удаляем предмет
            _diaryDbContext.Classes.Remove(classEntity);

            // Сохраняем изменения
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        public async Task<IActionResult> ManageAttendance(int classGroupId)
        {
            return RedirectToAction("ManageAttendance", "Shared", new { classGroupId = classGroupId });
        }

        // Сохранение столбика посещаемости старосте
        //[HttpPost]
        //public async Task<IActionResult> SaveAttendance([FromBody] List<AttendanceData> attendanceData)
        //{
        //    if (attendanceData == null || !attendanceData.Any())
        //    {
        //        return BadRequest("No attendance data submitted.");
        //    }

        //    foreach (var record in attendanceData)
        //    {
        //        var classGroupNumber = (await _diaryDbContext.ClassGroupAssignments.FindAsync(record.ClassGroupId)).GroupNumber;
               
        //        bool isAbsent = await _diaryDbContext.StudentAbsences
        //        .AnyAsync(sa => sa.StudentId == record.StudentId
        //                        && sa.GroupNumber == classGroupNumber
        //                        && sa.StartDate <= record.Date.ToDateTime(new TimeOnly())
        //                        && sa.EndDate >= record.Date.ToDateTime(new TimeOnly())
        //                        && sa.Status == AbsencesStatus.Approved);

        //        bool isPresent = record.IsPresent;

        //        if (isAbsent == isPresent)
        //        {
        //            isPresent = false;
        //        }

        //        // Ищем существующую запись посещаемости
        //        var existingRecord = await _diaryDbContext.Attendance
        //            .FirstOrDefaultAsync(a => a.ClassGroupId == record.ClassGroupId
        //                                   && a.StudentId == record.StudentId
        //                                   && a.Date == record.Date
        //                                   && a.SessionNumber == record.SessionNumber);

        //        if (existingRecord != null)
        //        {
        //            existingRecord.IsPresent = isPresent;
        //            existingRecord.IsAbsence = isAbsent;
        //            existingRecord.Status = AttendanceStatus.ConfirmedByTeacher;
        //        }
        //        else
        //        {
        //            _diaryDbContext.Attendance.Add(new AttendanceData
        //            {
        //                ClassGroupId = record.ClassGroupId,
        //                StudentId = record.StudentId,
        //                Date = record.Date,
        //                SessionNumber = record.SessionNumber,
        //                IsPresent = !isAbsent && isPresent,
        //                // Уважительная причина
        //                IsAbsence = isAbsent,
        //                Status = AttendanceStatus.ConfirmedByTeacher
        //            });
        //        }
        //    }
        //    await _diaryDbContext.SaveChangesAsync();
        //    return Ok();
        //}

        // Удаление столбика посещаемости
        [HttpPost]
        public async Task<IActionResult> DeleteAttendanceColumn(DateTime date, int sessionNumber)
        {
            // Convert DateTime to DateOnly for comparison
            DateOnly dateOnly = DateOnly.FromDateTime(date);

            var recordsToDelete = await _diaryDbContext.Attendance
                .Where(a => a.Date == dateOnly && a.SessionNumber == sessionNumber)
                .ToListAsync();

            if (recordsToDelete.Any())
            {
                _diaryDbContext.Attendance.RemoveRange(recordsToDelete);
                await _diaryDbContext.SaveChangesAsync();
            }

            return Ok();
        }

        // Метод отправки столбика посещаемости старосте
        //[HttpPost]
        //public async Task<IActionResult> SubmitAttendanceToGroupHead([FromBody] List<AttendanceData> attendanceData)
        //{
        //    if (attendanceData == null || !attendanceData.Any())
        //    {
        //        return BadRequest("No attendance data submitted.");
        //    }

        //    using (var transaction = await _diaryDbContext.Database.BeginTransactionAsync())
        //    {
        //        try
        //        {
        //            var classGroupNumber = (await _diaryDbContext.ClassGroupAssignments.FindAsync(attendanceData[0].ClassGroupId)).GroupNumber;

        //            foreach (var record in attendanceData)
        //            {
        //                var existingRecord = await _diaryDbContext.Attendance
        //                    .FirstOrDefaultAsync(a => a.ClassGroupId == record.ClassGroupId
        //                        && a.StudentId == record.StudentId
        //                        && a.Date == record.Date
        //                        && a.SessionNumber == record.SessionNumber);

        //                // пропуск если запись уже существует
        //                if (existingRecord != null)
        //                {
        //                    continue; 
        //                }
        //                // создание новой записи
                       
                                                
        //                bool isAbsent = await _diaryDbContext.StudentAbsences
        //                    .AnyAsync(sa => sa.StudentId == record.StudentId
        //                                    && sa.GroupNumber == classGroupNumber
        //                                    && sa.StartDate <= record.Date.ToDateTime(new TimeOnly())
        //                                    && sa.EndDate >= record.Date.ToDateTime(new TimeOnly())
        //                                    && sa.Status == AbsencesStatus.Approved);

        //                bool isPresent = record.IsPresent;

        //                if (isAbsent == isPresent)
        //                {
        //                    isPresent = false;
        //                }

        //                _diaryDbContext.Attendance.Add(new AttendanceData
        //                {
        //                    ClassGroupId = record.ClassGroupId,
        //                    StudentId = record.StudentId,
        //                    Date = record.Date,
        //                    IsPresent = isPresent,
        //                    IsAbsence = isAbsent,
        //                    Status = AttendanceStatus.Draft,
        //                    SessionNumber = record.SessionNumber
        //                });
        //            }

        //            await _diaryDbContext.SaveChangesAsync();
        //            await transaction.CommitAsync();
        //        }
        //        catch (Exception ex)
        //        {
        //            await transaction.RollbackAsync();
        //            Console.WriteLine(ex);
        //            return StatusCode(500, "Internal server error");
        //        }
        //    }

        //    return Ok();
        //}

        // Добавление занятия - привязка предмета к группе
        [HttpPost]
        public async Task<IActionResult> AssignClassToGroup(int classId, string groupNumber)
        {
            var existingClass = await _diaryDbContext.Classes.FindAsync(classId);
            if (existingClass == null)
            {
                return BadRequest(new { success = false, message = "Class not found" });
            }

           var newClassGroupAssignment = new ClassData()
           {
               ClassId = existingClass.ClassId,
               GroupNumber = groupNumber
           };

            _diaryDbContext.Classes.Add(newClassGroupAssignment);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Создание предмета
        [HttpPost]
        public async Task<IActionResult> CreateClassWithoutGroup(string subjectName, double studyDuration, int semester, string academicYear, string lessonType)
        {
            subjectName = subjectName.Trim();
            academicYear = academicYear.Trim();

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            // Получаем id преподавателя из базы данных
            var dbInstructorId = await _diaryDbContext.PersonContactUsers
                .Where(pcu => pcu.UserId == user.Id)
                .Select(pcu => pcu.PersonContactId)
                .FirstOrDefaultAsync();

            if (dbInstructorId == 0)
            {
                return BadRequest(new { success = false, message = "Instructor not found" });
            }

            if (!Enum.TryParse(lessonType, true, out LessonType parsedLessonType))
            {
                return BadRequest(new { success = false, message = "Invalid lesson type" });
            }

            // Проверка на существование аналогичного класса
            var existingClass = await _diaryDbContext.Classes
                .FirstOrDefaultAsync(c => c.Subject == subjectName
                    && c.InstructorId == dbInstructorId
                    && c.Type == parsedLessonType);

            if (existingClass != null)
            {
                return BadRequest(new { success = false, message = "Такое занятие уже существует!" });
            }

            var newClass = new ClassData
            {
                Subject = subjectName,
                InstructorId = dbInstructorId,               
                Semester = semester,
                AcademicYear = academicYear,
                Type = parsedLessonType
            };

            _diaryDbContext.Classes.Add(newClass);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupNumbers()
        {
            var groupNumbers = await _applicationDbContext.Groups
                .Select(g => g.Number)
                .Distinct()
                .ToListAsync();

            return Json(groupNumbers);
        }

        [HttpGet]
        public async Task<IActionResult> GetClasses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var dbInstructorId = await _diaryDbContext.PersonContactUsers
                .Where(pcu => pcu.UserId == user.Id)
                .Select(pcu => pcu.PersonContactId)
                .FirstOrDefaultAsync();

            var classes = await _diaryDbContext.Classes
                .Where(c => c.InstructorId == dbInstructorId)
                .Select(c => new
                {
                    classId = c.ClassId,
                    subject = c.Subject,
                    lessonType = c.Type.ToString(), // Преобразуем тип занятия в строку
                    academicYear = c.AcademicYear,
                    semester = c.Semester
                })
                .ToListAsync();

            return Json(classes);
        }

        // Метод получения данных предмета
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
                    lessonType = c.Type.ToString()
                })
                .FirstOrDefaultAsync();

            if (classData == null)
            {
                return NotFound();
            }

            return Json(classData);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateClass(int classId, string subjectName, double studyDuration, int semester, string academicYear, string lessonType)
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

            _diaryDbContext.Classes.Update(existingClass);
            await _diaryDbContext.SaveChangesAsync();

            return Json(new { success = true });
        }

    }
}

