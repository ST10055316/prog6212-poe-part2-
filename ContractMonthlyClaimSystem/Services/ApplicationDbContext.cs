using Microsoft.EntityFrameworkCore;
using ContractMonthlyClaimSystem.Models;

namespace ContractMonthlyClaimSystem.Services
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Claim> Claims { get; set; }
        public DbSet<Approval> Approvals { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Claim>()
                .HasOne(c => c.Lecturer)
                .WithMany()
                .HasForeignKey(c => c.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Approval>()
                .HasOne(a => a.Claim)
                .WithMany(c => c.Approvals)
                .HasForeignKey(a => a.ClaimId)
                .OnDelete(DeleteBehavior.Cascade);

            /*modelBuilder.Entity<Claim>()
        .Property(c => c.TotalAmount)
        .HasComputedColumnSql("[HoursWorked] * [HourlyRate]");*/

            modelBuilder.Entity<Approval>()
                .HasOne(a => a.Approver)
                .WithMany()
                .HasForeignKey(a => a.ApproverId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}