using diary.Models;
using System;
using System.Collections.Generic;

namespace diary.ViewModels
{
    public class ManageAttendanceViewModel
    {
        public int ClassId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectType { get; set; }
        public string GroupNumber { get; set; }
        public int TotalClasses { get; set; }
        public List<StudentData> Students { get; set; } = new List<StudentData>();
        public List<AttendanceData> AttendanceRecords { get; set; } = new List<AttendanceData>();
    }
}
