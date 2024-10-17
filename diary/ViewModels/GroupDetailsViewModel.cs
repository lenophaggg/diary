using diary.Models;
using System;
using System.Collections.Generic;

namespace diary.ViewModels
{
    public class AttendanceReport
    {
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string SubjectName { get; set; }
        public double AttendancePercentage { get; set; }
    }

    public class GroupDetailsViewModel
    {
        public string GroupNumber { get; set; }
        public List<StudentData> Students { get; set; }
        public StudentData GroupHead { get; set; }
        public List<AttendanceReport> AttendanceReports { get; set; }
    }
}
