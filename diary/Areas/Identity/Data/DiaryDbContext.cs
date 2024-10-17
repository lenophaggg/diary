using diary.Enums;
using diary.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
namespace diary.Data
{
    public class DiaryDbContext : DbContext
    {
        public DiaryDbContext(DbContextOptions<DiaryDbContext> options)
            : base(options)
        {
        }
        public DbSet<PersonContactUserData> PersonContactUsers { get; set; }
        public DbSet<StudentData> Students { get; set; }
        public DbSet<GroupHeadData> GroupHeads { get; set; }
        public DbSet<ClassData> Classes { get; set; }
        public DbSet<AttendanceData> Attendance { get; set; }
        public DbSet<StudentAbsencesData> StudentAbsences { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AttendanceData>()
                .Property(a => a.Date)
                .HasColumnType("date"); // Убедитесь, что колонка имеет тип date в базе данных

            base.OnModelCreating(modelBuilder);

        }
    }
}
