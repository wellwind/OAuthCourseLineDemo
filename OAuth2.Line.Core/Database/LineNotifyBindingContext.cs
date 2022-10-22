using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace OAuth2.Line.Core.Database
{
    public partial class LineNotifyBindingContext : DbContext
    {
        public LineNotifyBindingContext()
        {
        }

        public LineNotifyBindingContext(DbContextOptions<LineNotifyBindingContext> options)
            : base(options)
        {
        }

        public virtual DbSet<LineNotifyBinding> LineNotifyBindings { get; set; } = null!;
        public virtual DbSet<Message> Messages { get; set; } = null!;
        public virtual DbSet<MessageStatus> MessageStatuses { get; set; } = null!;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseMySql("server=localhost;port=5053;database=line_notify_binding;user=root;password=123456", Microsoft.EntityFrameworkCore.ServerVersion.Parse("10.8.3-mariadb"));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.UseCollation("utf8mb4_general_ci")
                .HasCharSet("utf8mb4");

            modelBuilder.Entity<LineNotifyBinding>(entity =>
            {
                entity.HasKey(e => e.Sub)
                    .HasName("PRIMARY");

                entity.ToTable("line_notify_binding");

                entity.Property(e => e.Sub)
                    .HasMaxLength(128)
                    .HasColumnName("sub");

                entity.Property(e => e.LineLoginAccessToken)
                    .HasMaxLength(512)
                    .HasColumnName("line_login_access_token");

                entity.Property(e => e.LineLoginIdToken)
                    .HasMaxLength(512)
                    .HasColumnName("line_login_id_token");

                entity.Property(e => e.LineLoginRefreshToken)
                    .HasMaxLength(512)
                    .HasColumnName("line_login_refresh_token");

                entity.Property(e => e.LineNotifyAccessToken)
                    .HasMaxLength(128)
                    .HasColumnName("line_notify_access_token");

                entity.Property(e => e.Name)
                    .HasMaxLength(128)
                    .HasColumnName("name");

                entity.Property(e => e.Picture)
                    .HasMaxLength(512)
                    .HasColumnName("picture");
            });

            modelBuilder.Entity<Message>(entity =>
            {
                entity.ToTable("message");

                entity.Property(e => e.Id)
                    .HasColumnType("int(11)")
                    .HasColumnName("id");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at")
                    .HasDefaultValueSql("'0000-00-00 00:00:00'");

                entity.Property(e => e.MessageText)
                    .HasColumnType("text")
                    .HasColumnName("message_text");

                entity.Property(e => e.StickerPackageId)
                    .HasColumnType("text")
                    .HasColumnName("sticker_package_id");

                entity.Property(e => e.StickerId)
                    .HasColumnType("text")
                    .HasColumnName("sticker_id");
            });

            modelBuilder.Entity<MessageStatus>(entity =>
            {
                entity.HasKey(e => new { e.MessageId, e.Sub })
                    .HasName("PRIMARY")
                    .HasAnnotation("MySql:IndexPrefixLength", new[] { 0, 0 });

                entity.ToTable("message_status");

                entity.HasIndex(e => e.Sub, "sub");

                entity.Property(e => e.MessageId)
                    .HasColumnType("int(11)")
                    .HasColumnName("message_id");

                entity.Property(e => e.Sub)
                    .HasMaxLength(128)
                    .HasColumnName("sub");

                entity.Property(e => e.CreatedAt)
                    .HasColumnType("datetime")
                    .HasColumnName("created_at");

                entity.Property(e => e.ErrorMessage)
                    .HasColumnType("text")
                    .HasColumnName("error_message");

                entity.Property(e => e.Status)
                    .HasColumnType("int(11)")
                    .HasColumnName("status");

                entity.HasOne(d => d.Message)
                    .WithMany(p => p.MessageStatuses)
                    .HasForeignKey(d => d.MessageId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("message_status_ibfk_2");

                entity.HasOne(d => d.SubNavigation)
                    .WithMany(p => p.MessageStatuses)
                    .HasForeignKey(d => d.Sub)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("message_status_ibfk_1");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
