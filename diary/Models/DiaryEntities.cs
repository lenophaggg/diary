using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using diary.Enums;

namespace diary.Models
{
    [Table("person_contact_users")]
    public class PersonContactUserData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public int Id { get; set; }
        [Column("personcontactid")]
        public int PersonContactId { get; set; }
        [Column("userid")]
        public string UserId { get; set; }
        public IdentityUser User { get; set; }
        public PersonContactData PersonContact { get; set; }
    }

    [Table("students")]
    public class StudentData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("studentid")]
        public int StudentId { get; set; }

        [Column("universitystudentid")]
        public string UniversityStudentId { get; set; }



        [Column("name")]
        public string Name { get; set; }

        [Column("groupnumber")]
        public string GroupNumber { get; set; }

        public GroupData Group { get; set; }
    }

    [Table("groupheads")]
    public class GroupHeadData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("groupheadid")]
        public int GroupHeadId { get; set; }

        [Column("studentid")]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public StudentData Student { get; set; }

        [Column("userid")]
        public string? UserId { get; set; }
        public IdentityUser User { get; set; }
    }

    [PrimaryKey("ClassId")]
    [Table("classes")]
    // Предметы, которые ведет преподаватель
    public class ClassData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("classid")]
        public int ClassId { get; set; }

        [Column("subjectname")]
        public string Subject { get; set; }

        [Column("instructorid")]
        public int InstructorId { get; set; }

        [Column("studyduration")]
        public double StudyDuration { get; set; }

        [Column("semester")]
        public int Semester { get; set; }

        [Column("academicyear")]
        public string AcademicYear { get; set; }

        [Column("typelesson")]
        public LessonType Type { get; set; }

        
    }


    [PrimaryKey("AttendanceId")]
    [Table("attendance")]
    public class AttendanceData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("attendanceid")]
        public int AttendanceId { get; set; }

        [Column("studentid")]
        public int StudentId { get; set; }

        [ForeignKey("StudentId")]
        public StudentData Student { get; set; }

        [Column("class_group_id")]
        public int ClassGroupId { get; set; }

        [ForeignKey("ClassGroupId")]
        public ClassGroupAssignmentData ClassGroup { get; set; }   

        [Column("date")]
        public DateOnly Date { get; set; } // Используем DateOnly для работы только с датами

        [Column("ispresent")]
        public bool IsPresent { get; set; }

        [Column("isabsence")]
        public bool IsAbsence { get; set; }

        [Column("status")]
        public AttendanceStatus Status { get; set; } // Новый флаг для статуса отметки

        [Column("sessionnumber")]
        public int SessionNumber { get; set; } // Новое поле для номера пары
    }

    public enum AttendanceStatus
    {
        Draft,              // Черновик, создается преподавателем
        ConfirmedByGroupHead,  // Отправлено старосте
        ConfirmedByTeacher     // Подтверждено преподавателем
    }    

    [Table("student_absences")]
    public class StudentAbsencesData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("requestid")]
        public int RequestId { get; set; }

        [Column("studentid")]
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public StudentData Student { get; set; }

        [Column("groupnumber")]
        public string GroupNumber { get; set; }

        [Column("reason")]
        public string Reason { get; set; }

        [Column("startdate", TypeName = "date")]
        public DateTime StartDate { get; set; }

        [Column("enddate", TypeName = "date")]
        public DateTime EndDate { get; set; }

        [Column("status")]
        public AbsencesStatus Status { get; set; }
    }

    public enum AbsencesStatus
    {
        Submitted,
        Approved,
        Rejected
    }

    [Table("class_group_assignment")]
    // Занятия по группам, которые ведет преподаватель
    public class ClassGroupAssignmentData
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("class_group_id")]
        public int ClassGroupId { get; set; }

        [Column("classid")]
        public int ClassId { get; set; }

        [ForeignKey("ClassId")]
        public ClassData Class { get; set; }

        [Column("groupnumber")]
        public string GroupNumber { get; set; }
    }
}
