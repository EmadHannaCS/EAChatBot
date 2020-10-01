using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace DAL.DB
{
    public partial class ChatBotDBContext : DbContext
    {
        public ChatBotDBContext()
        {
        }

        public ChatBotDBContext(DbContextOptions<ChatBotDBContext> options)
            : base(options)
        {
        }

        public virtual DbSet<UserSession> UserSession { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=.;Database=ChatBotDB;User ID=sa;Password=123456");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.HasIndex(e => e.UserPhone)
                    .HasName("Phone_unique")
                    .IsUnique();

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("(getdate())");

                entity.Property(e => e.LastSessionId)
                    .IsRequired()
                    .HasMaxLength(150);

                entity.Property(e => e.UserPhone)
                    .IsRequired()
                    .HasMaxLength(150);
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
