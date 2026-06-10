using Microsoft.EntityFrameworkCore;
using CVAnalyzer.Models;

namespace CVAnalyzer.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<CandidateCV> CandidateCVs { get; set; }
        public DbSet<JobRole> JobRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.HasPostgresExtension("vector");

            // 1-to-1 relationship: User <-> UserProfile
            modelBuilder.Entity<User>()
                .HasOne(u => u.UserProfile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Seed: Varsayılan iş pozisyonları
            modelBuilder.Entity<JobRole>().HasData(
                new JobRole { Id = 1, RoleName = "Software Developer", IconClass = "ti-code", Description = "Full-stack, frontend, backend", RequiredSkills = "C#,ASP.NET,SQL,JavaScript,HTML,CSS,Git,API,React,Docker" },
                new JobRole { Id = 2, RoleName = "Marketing Specialist", IconClass = "ti-speakerphone", Description = "Dijital, içerik, sosyal medya", RequiredSkills = "SEO,Google Ads,Analytics,Content Marketing,Social Media,Email Marketing,Canva,CRM" },
                new JobRole { Id = 3, RoleName = "Sales Manager", IconClass = "ti-chart-line", Description = "B2B, müşteri yönetimi", RequiredSkills = "CRM,Negotiation,B2B,Pipeline,Salesforce,KPI,Forecasting,Cold Calling" },
                new JobRole { Id = 4, RoleName = "Data Analyst", IconClass = "ti-chart-bar", Description = "SQL, Python, BI araçları", RequiredSkills = "SQL,Python,Power BI,Tableau,Excel,Statistics,Machine Learning,ETL,R" },
                new JobRole { Id = 5, RoleName = "UI/UX Designer", IconClass = "ti-palette", Description = "Figma, kullanıcı araştırması", RequiredSkills = "Figma,Adobe XD,User Research,Wireframing,Prototyping,CSS,Usability Testing,Design System" },
                new JobRole { Id = 6, RoleName = "Project Manager", IconClass = "ti-layout-kanban", Description = "Agile, Scrum, liderlik", RequiredSkills = "Agile,Scrum,Jira,Risk Management,Budgeting,Stakeholder Management,PMP,Leadership" }
            );
        }
    }
}
