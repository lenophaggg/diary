using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace diary.Models
{
    public class ScheduleOptions
    {
        public string TypeWeek { get; set; }
        // Другие свойства, если они есть
    }

    public class AcademicSettings
    {
        public string CurrentAcademicYear { get; set; }
        public int CurrentSemester { get; set; }
    }

    [PrimaryKey("ScheduleId")]
    [Table("schedules")]
    public class ScheduleData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("scheduleid")]
        public int ScheduleId { get; set; } // Первичный ключ

        [Column("dayofweek")]
        public string? DayOfWeek { get; set; } // День недели

        [Column("starttime")]
        public TimeSpan? StartTime { get; set; } // Время начала

        [Column("endtime")]
        public TimeSpan? EndTime { get; set; } // Время конца

        [Column("weektype")]
        public string? WeekType { get; set; } // Тип недели (верхняя, нижняя, обе)

        [Column("classroomnumber")]
        public string? Classroom { get; set; } // Аудитория
        [ForeignKey("Classroom")]
        public ClassroomData ClassroomNumber { get; set; }

        [Column("groupnumber")]
        public string? Group { get; set; } // Группа
        [ForeignKey("Group")]
        public GroupData GroupNumber { get; set; }

        [Column("subjectname")]
        public string? Subject { get; set; }
        [ForeignKey("Subject")]
        public SubjectData SubjectName { get; set; }

        [Column("instructorid")]
        public int? InstructorId { get; set; }
        [ForeignKey("InstructorId")]
        public PersonContactData Instructor { get; set; }

        [Column("scheduleinfo")]
        public string? ScheduleInfo { get; set; }
    }

    [PrimaryKey("IdContact")]
    [Table("person_contacts")]
    public class PersonContactData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("idcontact")]
        public int IdContact { get; set; }

        [Column("university_idcontact")]
        public string UniversityIdContact { get; set; }

        [Column("namecontact")]
        public string NameContact { get; set; }

        [Column("position")]
        public string[]? Position { get; set; }

        [Column("academicdegree")]
        public string? AcademicDegree { get; set; }

        [Column("teachingexperience")]
        public string? TeachingExperience { get; set; }

        [Column("telephone")]
        public string? Telephone { get; set; }

        [Column("email")]
        public string? Email { get; set; }

        [Column("information")]
        public string? Information { get; set; }

        [Column("imgpath")]
        public string? ImgPath { get; set; }

        // Навигационное свойство для связанных предметов, преподаваемых контактом
        public ICollection<PersonTaughtSubjectData> TaughtSubjects { get; set; }     

    }

    [PrimaryKey("SubjectName")]
    [Table("subjects")]
    public class SubjectData
    {
        [Column("subjectname")]
        public string SubjectName { get; set; }
    }

    [PrimaryKey("IdContact", "SubjectName")]
    [Table("person_taughtsubjects")]
    public class PersonTaughtSubjectData
    {
        [Column("idcontact")]
        public int IdContact { get; set; }
        [Column("subjectname")]
        public string SubjectName { get; set; }

        [ForeignKey("IdContact")]
        public PersonContactData Person { get; set; }

        [ForeignKey("SubjectName")]
        public SubjectData Subject { get; set; }
    }

    [PrimaryKey("Name")]
    [Table("faculties")]
    public class FacultyData
    {
        [Column("name")]
        public string Name { get; set; }
    }

    [PrimaryKey("Number")]
    [Table("groups")]
    public class GroupData
    {
        [Column("groupnumber")]
        public string Number { get; set; }
        [Column("facultyname")]
        public string? FacultyName { get; set; }
        [ForeignKey("FacultyName")]
        public FacultyData Faculty { get; set; }
    }
    [Table("actual_groups")]
    [PrimaryKey("GroupNumber")]
    public class ActualGroup
    {
        [Column("groupnumber")]
        public string GroupNumber { get; set; }
    }

    [PrimaryKey("ClassroomNumber")]
    [Table("classrooms")]
    public class ClassroomData
    {
        [Column("classroomnumber")]
        public string ClassroomNumber { get; set; }
    }

}
