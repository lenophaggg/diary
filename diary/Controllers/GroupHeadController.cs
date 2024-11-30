using diary.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using diary.ViewModels;
using diary.Models;
using Azure.Core;
using diary.Enums;

namespace diary.Controllers
{
    [Authorize(Roles = "GroupHead")]
    public class GroupHeadController : Controller
    {
        private readonly ApplicationDbContext _applicationDbContext;
        private readonly DiaryDbContext _diaryDbContext;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public GroupHeadController(ApplicationDbContext applicationDbContext,
            DiaryDbContext diaryDbContext,
            UserManager<IdentityUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _applicationDbContext = applicationDbContext;
            _diaryDbContext = diaryDbContext;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: GroupHeadController
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var groupHeadUser = await _diaryDbContext.GroupHeads
                                            .FirstOrDefaultAsync(g => g.UserId == user.Id);
            if (groupHeadUser == null)
            {
                return NotFound("Group Head not found");
            }

            var studentGroupHead = await _diaryDbContext.Students
                .FirstOrDefaultAsync(s => s.StudentId == groupHeadUser.StudentId);

            return View(studentGroupHead);
        }

        [HttpGet]
        public async Task<IActionResult> GetInstructors()
        {
            try
            {
                // Fetch all instructors from the PersonContacts table
                var instructors = await _applicationDbContext.PersonContacts
                    .Select(pc => new
                    {
                        idContact = pc.IdContact,          // Instructor ID
                        nameContact = pc.NameContact       // Instructor name
                    })
                    .ToListAsync();

                // Return the list of instructors as JSON
                return Json(instructors);
            }
            catch (Exception ex)
            {
                // Handle any errors and return a server error response
                return StatusCode(500, new { success = false, message = "Ошибка при получении списка преподавателей" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateClass(string subjectName, double studyDuration, int semester, string academicYear, string lessonType)
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
                    && c.Type == parsedLessonType
                    && c.AcademicYear == academicYear
                    && c.Semester == semester);

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
        public async Task<IActionResult> GetClass(int classId)
        {
            // Get class data from DiaryDbContext
            var classData = await _diaryDbContext.Classes
                .Where(c => c.ClassId == classId)
                .Select(c => new
                {
                    classId = c.ClassId,
                    subject = c.Subject,
                    semester = c.Semester,
                    academicYear = c.AcademicYear,
                    lessonType = c.Type.ToString(),
                    instructorId = c.InstructorId
                })
                .FirstOrDefaultAsync();

            if (classData == null)
            {
                return NotFound();
            }

            // Get instructor's name from ApplicationDbContext
            var instructor = await _applicationDbContext.PersonContacts
                .FirstOrDefaultAsync(pc => pc.IdContact == classData.instructorId);

            var instructorName = instructor?.NameContact ?? "";

            // Return combined result
            return Json(new
            {
                classId = classData.classId,
                subject = classData.subject,
                semester = classData.semester,
                academicYear = classData.academicYear,
                lessonType = classData.lessonType,
                instructorName = instructorName
            });
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

        public async Task<IActionResult> ListClasses()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized();
            }

            var groupHead = await _diaryDbContext.GroupHeads
              .Include(gh => gh.Student)
              .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

            if (groupHead == null)
            {
                return NotFound("Group head not found");
            }

            var groupNumber = groupHead.Student.GroupNumber;

            var groupClasses = await _diaryDbContext.Classes
                .Where(c => c.GroupNumber == groupNumber)             
                .ToListAsync();

           var instructors = await _applicationDbContext
                .PersonContacts
                .ToListAsync();

            var classWithInstructors = groupClasses.Select(c => new
            {
                ClassId = c.ClassId,
                Subject = c.Subject,
                InstructorName = instructors.FirstOrDefault(instr => instr.IdContact == c.InstructorId)?.NameContact ?? "No Instructor Assigned",
                Semester = c.Semester,
                AcademicYear = c.AcademicYear,
                Type = c.Type, // Lesson type (for data-lesson-type attribute)
            }).ToList();

            ViewBag.GroupNumber = $"{groupNumber}";

            // Pass the data to the view
            return View(classWithInstructors);
        }

        // Метод отображения карточки группы, к которой принадлежит староста
        public async Task<IActionResult> MyGroup()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var groupHead = await _diaryDbContext.GroupHeads
                .Include(gh => gh.Student)
                .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

            if (groupHead == null)
            {
                return NotFound("Group head not found");
            }

            var groupNumber = groupHead.Student.GroupNumber;

            var students = await _diaryDbContext.Students
                .Where(s => s.GroupNumber == groupNumber)
                .ToListAsync();

            var groupDetailsViewModel = new GroupDetailsViewModel
            {
                GroupNumber = groupNumber,
                Students = students,
                GroupHead = groupHead.Student
            };

            return View(groupDetailsViewModel);
        }

        [HttpPost]
        public async Task<IActionResult> AddStudent(string studentName, string universityStudentId, string groupNumber)
        {
            var curUser = await _userManager.GetUserAsync(User);
            if (curUser == null)
            {
                return Unauthorized();
            }

            if (studentName.Trim().Split(' ').Length != 3)
            {
                return BadRequest("Имя студента должно состоять из трех слов");
            }

            // Проверка на то, что universityStudentId состоит только из цифр
            if (!universityStudentId.All(char.IsDigit))
            {
                return BadRequest("Номер студенческого билета должен состоять только из цифр");
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
        [HttpPost]
        public async Task<IActionResult> RemoveStudent(int studentId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return NotFound("User not found");
            }

            var groupHead = await _diaryDbContext.GroupHeads
                .FirstOrDefaultAsync(gh => gh.UserId == user.Id);

            if (groupHead == null || groupHead.StudentId == studentId)
            {
                return BadRequest("Cannot remove yourself as a group head");
            }

            var student = await _diaryDbContext.Students.FindAsync(studentId);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            _diaryDbContext.Students.Remove(student);
            await _diaryDbContext.SaveChangesAsync();

            return Ok(new { success = true });
        }
        // Метод отображения занятий по группам
        public async Task<IActionResult> Classes()
        {
            return RedirectToAction("Classes", "Shared");            
        }
        #region Attendance
        public async Task<IActionResult> ManageAttendance(int ClassGroupId)
        {
            return RedirectToAction("ManageAttendance", "Shared", new { ClassGroupId = ClassGroupId });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitAttendanceToTeacher([FromBody] List<AttendanceData> attendanceData)
        {
            if (attendanceData == null || !attendanceData.Any())
            {
                return BadRequest("No attendance data submitted.");
            }

            foreach (var record in attendanceData)
            {
                var existingRecord = await _diaryDbContext.Attendance
                    .Include(a => a.Class)
                    .FirstOrDefaultAsync(a => a.ClassId == record.ClassId
                                           && a.StudentId == record.StudentId
                                           && a.Date == record.Date
                                           && a.SessionNumber == record.SessionNumber);

                if (existingRecord != null)
                {                    
                    var classGroupNumber = existingRecord.Class.GroupNumber;

                    bool isAbsent = await _diaryDbContext.StudentAbsences
                        .AnyAsync(sa => sa.StudentId == record.StudentId
                                        && sa.GroupNumber == classGroupNumber
                                        && sa.StartDate <= record.Date.ToDateTime(new TimeOnly())
                                        && sa.EndDate >= record.Date.ToDateTime(new TimeOnly())
                                        && sa.Status == AbsencesStatus.Approved);

                    bool isPresent = record.IsPresent;

                    if (isAbsent == isPresent)
                    {
                        isPresent = false;
                    }

                    // Обновляем значения IsPresent и IsAbsence в зависимости от данных об отсутствии
                    existingRecord.IsPresent = isPresent;
                    existingRecord.IsExcusedAbsence = isAbsent;
                   
                }
            }

            await _diaryDbContext.SaveChangesAsync();
            return Ok(new { success = true });
        }


        #endregion
        public async Task<IActionResult> StudentAbsences()
        {
            return RedirectToAction("StudentAbsences", "Shared");
        }        

        public async Task<IActionResult> CreateStudentAbsenceRequest()
        {
            return RedirectToAction("CreateStudentAbsenceRequest", "Shared");
        }


    }

}
