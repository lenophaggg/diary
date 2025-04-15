using diary.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace diary.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<ScheduleData> Schedules { get; set; }

        public DbSet<PersonContactData> PersonContacts { get; set; }

        public DbSet<PersonTaughtSubjectData> PersonTaughtSubjects { get; set; }
        public DbSet<SubjectData> Subjects { get; set; }

        public DbSet<FacultyData> Faculties { get; set; }
        public DbSet<GroupData> Groups { get; set; }
        public DbSet<ActualGroup> ActualGroups { get; set; }

        public DbSet<ClassroomData> Classrooms { get; set; }

       
    }
}
