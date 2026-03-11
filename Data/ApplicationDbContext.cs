using Microsoft.EntityFrameworkCore;
using InternetAccessManager.Api.Entities;

namespace InternetAccessManager.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Audience> Audiences { get; set; }
    public DbSet<Computer> Computers { get; set; }
    public DbSet<AssignedAudience> AssignedAudiences { get; set; }
    public DbSet<ActionLog> ActionLogs { get; set; }
    public DbSet<SystemLock> SystemLocks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Уникальные индексы
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Username)
            .IsUnique();

        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<Computer>()
            .HasIndex(c => c.IPAddress);

        // Связи между таблицами
        modelBuilder.Entity<Computer>()
            .HasOne<Audience>()
            .WithMany()
            .HasForeignKey(c => c.AudienceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignedAudience>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(aa => aa.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AssignedAudience>()
            .HasOne<Audience>()
            .WithMany()
            .HasForeignKey(aa => aa.AudienceId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ActionLog>()
            .HasOne<User>()
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}