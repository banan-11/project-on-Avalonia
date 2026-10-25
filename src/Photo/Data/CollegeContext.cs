using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Photo.Models;

namespace Photo.Data;

public partial class CollegeContext : DbContext
{
    public CollegeContext(DbContextOptions<CollegeContext> options): base(options)
    {
    }

    public virtual DbSet<Hall> Halls { get; set; }

    public virtual DbSet<Order> Orders { get; set; }

    public virtual DbSet<Photographer> Photographers { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Status> Statuses { get; set; }

    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Hall>(entity =>
        {
            entity.HasKey(e => e.HallId).HasName("PK__Halls__A63DE8CF32ED47E7");

            entity.Property(e => e.HallId).HasColumnName("hall_id");
            entity.Property(e => e.EquipmentDescription).HasColumnName("equipment_description");
            entity.Property(e => e.ImagePath)
                .HasMaxLength(255)
                .HasColumnName("image_path");
            entity.Property(e => e.Name)
                .HasMaxLength(100)
                .HasColumnName("name");
        });

        modelBuilder.Entity<Order>(entity =>
        {
            entity.HasKey(e => e.OrderId).HasName("PK__Orders__4659622980C4D28F");

            entity.Property(e => e.OrderId).HasColumnName("order_id");
            entity.Property(e => e.BookingDate).HasColumnName("booking_date");
            entity.Property(e => e.CreatedAt)
                .HasDefaultValueSql("(sysdatetime())")
                .HasColumnName("created_at");
            entity.Property(e => e.HallId).HasColumnName("hall_id");
            entity.Property(e => e.HoursCount)
                .HasColumnType("decimal(4, 2)")
                .HasColumnName("hours_count");
            entity.Property(e => e.PhotographerId).HasColumnName("photographer_id");
            entity.Property(e => e.StartTime).HasColumnName("start_time");
            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.Hall).WithMany(p => p.Orders)
                .HasForeignKey(d => d.HallId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__hall_id__42E1EEFE");

            entity.HasOne(d => d.Photographer).WithMany(p => p.Orders)
                .HasForeignKey(d => d.PhotographerId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__photogra__43D61337");

            entity.HasOne(d => d.Status).WithMany(p => p.Orders)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__status_i__40058253");

            entity.HasOne(d => d.User).WithMany(p => p.Orders)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Orders__user_id__41EDCAC5");
        });

        modelBuilder.Entity<Photographer>(entity =>
        {
            entity.HasKey(e => e.PhotographerId).HasName("PK__Photogra__1C0EB3EFFAA7F5F8");

            entity.Property(e => e.PhotographerId).HasColumnName("photographer_id");
            entity.Property(e => e.Price).HasColumnName("price");
            entity.Property(e => e.UserId).HasColumnName("user_id");

            entity.HasOne(d => d.User).WithMany(p => p.Photographers)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Photograp__user___3493CFA7");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.RoleId).HasName("PK__Roles__760965CC613E23FC");

            entity.HasIndex(e => e.RoleName, "UQ__Roles__783254B1A52CAA8F").IsUnique();

            entity.Property(e => e.RoleId).HasColumnName("role_id");
            entity.Property(e => e.RoleName)
                .HasMaxLength(50)
                .HasColumnName("role_name");
        });

        modelBuilder.Entity<Status>(entity =>
        {
            entity.HasKey(e => e.StatusId).HasName("PK__status__3683B531A700F2CC");

            entity.ToTable("status");

            entity.Property(e => e.StatusId).HasColumnName("status_id");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasDefaultValue("Подтверждён")
                .HasColumnName("name");
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.UserId).HasName("PK__Users__B9BE370F4A0BE447");

            entity.HasIndex(e => e.Login, "UQ__Users__7838F27251CCFA9B").IsUnique();

            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.Email)
                .HasMaxLength(255)
                .HasColumnName("email");
            entity.Property(e => e.LastName)
                .HasMaxLength(255)
                .HasColumnName("last_name");
            entity.Property(e => e.Login)
                .HasMaxLength(50)
                .HasColumnName("login");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasColumnName("name");
            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .HasColumnName("password_hash");
            entity.Property(e => e.Phone).HasColumnName("phone");
            entity.Property(e => e.RoleId).HasColumnName("role_id");

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("FK__Users__role_id__31B762FC");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
