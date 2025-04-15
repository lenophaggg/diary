using diary.Models;
using System;
using System.Collections.Generic;

namespace diary.ViewModels
{
    public class AttendanceReport
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string SubjectName { get; set; }
        public string LessonType { get; set; } // Например, "Лекция", "Практика"
        public string AcademicYear { get; set; }
        public int Semester { get; set; }
        public double AttendancePercentage { get; set; } // Можно использовать для хранения процента, если нужно
        public DateOnly Date { get; set; }
        public bool IsPresent { get; set; }
        public bool IsExcusedAbsence { get; set; }
        public int SessionNumber { get; set; }
    }

    public class MonthlyAttendanceReport
    {
        public string StudentId { get; set; }
        public string StudentName { get; set; }
        public string Month { get; set; } // Например, "Март 2025"
        public double AttendancePercentage { get; set; }
    }

    public class GroupDetailsViewModel
    {
        public string GroupNumber { get; set; }
        public List<StudentData> Students { get; set; }
        public StudentData GroupHead { get; set; }
        public List<AttendanceReport> AttendanceReports { get; set; }

        // Поле для журнала посещаемости (данные по месяцам, датам и предметам)
        public List<AttendanceReport> AttendanceJournal { get; set; } = new List<AttendanceReport>();

        // Добавляем поле для месячных отчетов о посещаемости
        public List<MonthlyAttendanceReport> MonthlyAttendanceReports { get; set; } = new List<MonthlyAttendanceReport>();
    }

}
