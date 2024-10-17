using diary.Enums;
using diary.Models;

namespace diary.ViewModels
{
    public class ClassViewModel
    {
        public List<ClassViewItem> Classes { get; set; }
    }

    public class ClassViewItem
    {
        public int ClassId { get; set; }
        public string Subject { get; set; }
        public int Semester { get; set; }
        public string AcademicYear { get; set; }
        public LessonType Type { get; set; }
        public string GroupNumber { get; set; }
        public string InstructorName { get; set; }
    }


}
