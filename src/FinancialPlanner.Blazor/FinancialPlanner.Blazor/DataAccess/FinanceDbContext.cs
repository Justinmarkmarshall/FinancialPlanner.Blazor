using FinancialPlanner.Blazor.DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using static FinancialPlanner.Blazor.Components.Models.Enums.Enums;

namespace FinancialPlanner.Blazor.DataAccess
{
    public class FinanceDbContext(DbContextOptions<FinanceDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserProfile> UserProfiles { get; set; } = null!;
        public DbSet<UserSession> UserSessions { get; set; } = null!;
        public DbSet<Month> Months { get; set; } = null!;
        public DbSet<Expenditure> Expenditures { get; set; } = null!;
        public DbSet<Income> Incomes { get; set; } = null!;
        public DbSet<BankStatement> BankStatements { get; set; } = null!;
        public DbSet<IncomeCategory> IncomeCategories { get; set; } = null!;
        public DbSet<ExpenseCategory> ExpenseCategories { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User has unique GoogleSubject
            modelBuilder.Entity<User>()
                .HasIndex(u => u.GoogleSubject)
                .IsUnique();

            // User <-> UserProfile: one-to-one
            modelBuilder.Entity<User>()
                .HasOne(u => u.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId);

            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasKey(s => s.SessionId);

                entity.HasOne(s => s.User)
                      .WithMany(u => u.Sessions)
                      .HasForeignKey(s => s.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // SQLite-friendly GUID storage (optional but recommended)
                entity.Property(s => s.SessionId)
                      .HasConversion<string>();

                entity.Property(s => s.CreatedUtc).IsRequired();
                entity.Property(s => s.ExpiresUtc).IsRequired();

                entity.Property(s => s.IpAddress).HasMaxLength(45);    // fits IPv6 text
                entity.Property(s => s.UserAgent).HasMaxLength(512);   // pragmatic cap

                entity.HasIndex(s => s.UserId);
                entity.HasIndex(s => s.ExpiresUtc);
            });


            // Index for session lookup
            modelBuilder.Entity<UserSession>()
                .HasIndex(s => new { s.SessionId, s.UserId });

            // User -> Months: one-to-many
            modelBuilder.Entity<Month>()
                .HasOne(m => m.User)
                .WithMany(u => u.Months)
                .HasForeignKey(m => m.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // User -> Incomes: one-to-many
            modelBuilder.Entity<Income>()
                .HasOne(i => i.User)
                .WithMany(u => u.Incomes)
                .HasForeignKey(i => i.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> Expenditures: one-to-many
            modelBuilder.Entity<Expenditure>()
                .HasOne(e => e.User)
                .WithMany(u => u.Expenditures)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> BankStatements: one-to-many
            modelBuilder.Entity<BankStatement>()
                .HasOne(b => b.User)
                .WithMany(u => u.BankStatements)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> IncomeCategories: one-to-many
            modelBuilder.Entity<Models.IncomeCategory>()
                .HasOne(c => c.User)
                .WithMany(u => u.IncomeCategories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> ExpenseCategories: one-to-many
            modelBuilder.Entity<ExpenseCategory>()
                .HasOne(c => c.User)
                .WithMany(u => u.ExpenseCategories)
                .HasForeignKey(c => c.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Month -> BankStatements: one-to-many
            modelBuilder.Entity<BankStatement>()
                .HasOne(b => b.Month)
                .WithMany(m => m.BankStatements)
                .HasForeignKey(b => b.MonthId)
                .OnDelete(DeleteBehavior.Restrict);

            // Month -> Incomes: one-to-many
            modelBuilder.Entity<Income>()
                .HasOne(i => i.Month)
                .WithMany(m => m.Incomes)
                .HasForeignKey(i => i.MonthId)
                .OnDelete(DeleteBehavior.Restrict);

            // Month -> Expenditures: one-to-many
            modelBuilder.Entity<Expenditure>()
                .HasOne(e => e.Month)
                .WithMany(m => m.Expenditures)
                .HasForeignKey(e => e.MonthId)
                .OnDelete(DeleteBehavior.Restrict);

            // IncomeCategory -> Incomes: one-to-many
            modelBuilder.Entity<Income>()
                .HasOne(i => i.Category)
                .WithMany(c => c.Incomes)
                .HasForeignKey(i => i.IncomeCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // ExpenseCategory -> Expenditures: one-to-many
            modelBuilder.Entity<Expenditure>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Expenditures)
                .HasForeignKey(e => e.ExpenseCategoryId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
