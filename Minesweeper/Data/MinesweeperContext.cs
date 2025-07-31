using Microsoft.EntityFrameworkCore;
using Minesweeper.Models;

namespace Minesweeper.Data
{
    public class MinesweeperContext : DbContext
    {
        public MinesweeperContext(DbContextOptions<MinesweeperContext> options) : base(options)
        {
        }

        public DbSet<Player> Players { get; set; }
        public DbSet<GameResult> GameResults { get; set; }
        public DbSet<GameSession> GameSessions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Player configuration
            modelBuilder.Entity<Player>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Name).IsUnique();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            });

            // GameResult configuration
            modelBuilder.Entity<GameResult>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Player)
                      .WithMany(p => p.GameResults)
                      .HasForeignKey(e => e.PlayerId)
                      .OnDelete(DeleteBehavior.Cascade);
                
                entity.Property(e => e.Difficulty).HasConversion<int>();
                entity.Property(e => e.CompletionTime).HasConversion(
                    v => v.TotalMilliseconds,
                    v => TimeSpan.FromMilliseconds(v));
            });

            // GameSession configuration
            modelBuilder.Entity<GameSession>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasOne(e => e.Player)
                      .WithMany()
                      .HasForeignKey(e => e.PlayerId)
                      .OnDelete(DeleteBehavior.SetNull);
                
                entity.Property(e => e.Difficulty).HasConversion<int>();
                entity.Property(e => e.GameBoardJson).IsRequired();
            });
        }
    }
}