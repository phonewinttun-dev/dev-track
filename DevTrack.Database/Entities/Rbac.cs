using Microsoft.EntityFrameworkCore;
namespace DevTrack.Database.Entities;

public class AppUser { public int Id { get; set; } public string Username { get; set; } = ""; public string DisplayName { get; set; } = ""; public string PasswordHash { get; set; } = ""; public int RoleId { get; set; } public AppRole Role { get; set; } = null!; public bool IsActive { get; set; } = true; }
public class AppRole { public int Id { get; set; } public string RoleName { get; set; } = ""; }
public class AppPermission { public int Id { get; set; } public string PermissionName { get; set; } = ""; }
public class AppRolePermission { public int RoleId { get; set; } public AppRole Role { get; set; } = null!; public int PermissionId { get; set; } public AppPermission Permission { get; set; } = null!; }
public partial class DevTrackDbContext
{
    public DbSet<AppUser> Users => Set<AppUser>();
    public DbSet<AppRole> Roles => Set<AppRole>();
    public DbSet<AppPermission> Permissions => Set<AppPermission>();
    public DbSet<AppRolePermission> RolePermissions => Set<AppRolePermission>();
    partial void OnModelCreatingPartial(Microsoft.EntityFrameworkCore.ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>().HasIndex(x => x.Username).IsUnique();
        modelBuilder.Entity<AppUser>().Property(x => x.Username).HasMaxLength(150);
        modelBuilder.Entity<AppRole>().HasIndex(x => x.RoleName).IsUnique();
        modelBuilder.Entity<AppRole>().Property(x => x.RoleName).HasMaxLength(100);
        modelBuilder.Entity<AppPermission>().HasIndex(x => x.PermissionName).IsUnique();
        modelBuilder.Entity<AppPermission>().Property(x => x.PermissionName).HasMaxLength(100);
        modelBuilder.Entity<AppRolePermission>().HasKey(x => new { x.RoleId, x.PermissionId });
    }
}
