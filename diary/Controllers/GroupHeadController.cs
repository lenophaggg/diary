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
                    .Include(a => a.ClassGroup)
                    .FirstOrDefaultAsync(a => a.ClassGroupId == record.ClassGroupId
                                           && a.StudentId == record.StudentId
                                           && a.Date == record.Date
                                           && a.SessionNumber == record.SessionNumber);

                if (existingRecord != null)
                {                    
                    var classGroupNumber = existingRecord.ClassGroup.GroupNumber;

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
                    existingRecord.IsAbsence = isAbsent;
                    existingRecord.Status = AttendanceStatus.ConfirmedByGroupHead;
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
