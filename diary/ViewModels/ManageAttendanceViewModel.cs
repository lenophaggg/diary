using diary.Models;
using System;
using System.Collections.Generic;

namespace diary.ViewModels
{
    public class ManageAttendanceViewModel
    {
        public int ClassGroupId { get; set; }
        public string SubjectName { get; set; }
        public string SubjectType { get; set; }
        public string GroupNuber { get; set; }

        public double StudyDuration { get; set; }

        public List<StudentData> Students { get; set; } = new List<StudentData>();
        public List<AttendanceData> AttendanceRecords { get; set; } = new List<AttendanceData>();

        // We can add any helper methods or additional properties as needed
    }
}
