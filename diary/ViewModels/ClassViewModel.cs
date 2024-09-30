using diary.Models;

namespace diary.ViewModels
{
    public class ClassViewModel
    {
        public List<ClassGroupAssignmentData> ClassGroupAssignments { get; set; }
        //для преподавателей
        public List<string> GroupsNumbers { get; set; }
    }

    
}
