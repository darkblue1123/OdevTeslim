using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace OdevTeslim.Models
{
    public class AppDbContext : IdentityDbContext<AppUser, AppRole, string>
    {

        public AppDbContext(DbContextOptions<AppDbContext> options)
             : base(options)
        {
        }

        // DbSet'ler - Tipler doğru
        public DbSet<Course> Courses { get; set; }
        public DbSet<Assignment> Assignments { get; set; }
        public DbSet<Submission> Submissions { get; set; }
        public DbSet<CourseEnrollment> Enrollments { get; set; }


        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder); 


            builder.Entity<CourseEnrollment>()
                .HasIndex(e => new { e.StudentId, e.CourseId }) // StudentId ve CourseId kombinasyonu unique olsun
                .IsUnique();

            // --- İlişki Tanımlamaları (ForeignKey isimleri doğruysa genelde otomatik algılanır, ama teyit etmek iyidir) ---

            // Enrollment ilişkileri (StudentId string, CourseId int)
            builder.Entity<CourseEnrollment>()
                .HasOne(e => e.Student)
                .WithMany(u => u.Enrollments)
                .HasForeignKey(e => e.StudentId) // string FK
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<CourseEnrollment>()
                .HasOne(e => e.Course)
                .WithMany(c => c.Enrollments)
                .HasForeignKey(e => e.CourseId) // int FK
                 .OnDelete(DeleteBehavior.Restrict);


            // Öğretmen (User - string Id) ve Kurs (Course - int Id) ilişkisi
            builder.Entity<Course>()
                .HasOne(c => c.Teacher)
                .WithMany(t => t.TaughtCourses)
                .HasForeignKey(c => c.TeacherId) // string FK
                .OnDelete(DeleteBehavior.Restrict);

            // Kurs (int Id) ve Ödev (Assignment - int Id) ilişkisi
            builder.Entity<Assignment>()
                .HasOne(a => a.Course)
                .WithMany(c => c.Assignments)
                .HasForeignKey(a => a.CourseId) // int FK
                .OnDelete(DeleteBehavior.Cascade);

            // Ödev (int Id) ve Gönderim (Submission - int Id) ilişkisi
            builder.Entity<Submission>()
               .HasOne(s => s.Assignment)
               .WithMany(a => a.Submissions)
               .HasForeignKey(s => s.AssignmentId) // int FK
               .OnDelete(DeleteBehavior.Cascade);

            // Öğrenci (User - string Id) ve Gönderim (Submission - int Id) ilişkisi
            builder.Entity<Submission>()
                .HasOne(s => s.Student)
                .WithMany(u => u.Submissions)
                .HasForeignKey(s => s.StudentId) // string FK
                .OnDelete(DeleteBehavior.Restrict);

            // Notu veren Öğretmen (User - string Id) ve Gönderim (Submission - int Id) ilişkisi
            builder.Entity<Submission>()
                .HasOne(s => s.GradedByTeacher)
                .WithMany() // ApplicationUser'da direkt koleksiyon tanımlamadık
                .HasForeignKey(s => s.GradedByTeacherId) // string FK (nullable)
                .OnDelete(DeleteBehavior.SetNull); // Öğretmen silinirse FK null olsun

            // BaseEntity'deki CreatedDate/ModifiedDate için otomatik değer atamaları gibi
            // ek konfigürasyonları da burada veya SaveChanges interceptor'ları ile yapabilirsiniz.
        }


    }


}
