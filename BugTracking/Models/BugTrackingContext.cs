using Microsoft.EntityFrameworkCore;

namespace BugTracking.Models
{
    public class BugTrackingContext : DbContext
    {
        public BugTrackingContext(DbContextOptions<BugTrackingContext> options)
            : base(options)
        {
        }

        public DbSet<TaskItem> TaskItem { get; set; }
        public DbSet<ProjectItem> ProjectItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ProjectItem>();
            modelBuilder.Entity<TaskItem>();
        }
    }
}