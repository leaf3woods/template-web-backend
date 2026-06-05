using CaseExtensions;
using Microsoft.EntityFrameworkCore;
using Template.Web.Domain.Entities;
using Template.Web.Domain.Entities.Account;
using Template.Web.Domain.Entities.Authority;
using Template.Web.Domain.Entities.Base;

namespace Template.Web.Infrastructure.DbContexts
{
    public class ApiDbContext : DbContext
    {
        public ApiDbContext(DbContextOptions<ApiDbContext> options)
            : base(options) { }

        #region dbsets

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }

        #endregion dbsets

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            foreach (var entityEntry in ChangeTracker.Entries())
            {
                if (
                    entityEntry.State == EntityState.Deleted
                    && entityEntry.Entity is ISoftDelete delete
                )
                {
                    entityEntry.State = EntityState.Unchanged;
                    delete.SoftDeleted = true;
                    delete.DeleteTime = DateTime.UtcNow;
                }
            }
            return base.SaveChangesAsync(cancellationToken);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            #region table prefix

            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                entityType.SetTableName(entityType.ClrType.Name.ToSnakeCase());
            }

            #endregion table prefix

            #region soft delete filter

            foreach (
                var entityType in modelBuilder
                    .Model.GetEntityTypes()
                    .Where(t => typeof(ISoftDelete).IsAssignableFrom(t.ClrType))
            )
            {
                entityType.AddSoftDeleteQueryFilter();
            }

            #endregion soft delete filter

            #region entities initialize

            #region role

            modelBuilder.Entity<Role>().HasData(Role.Seeds);

            modelBuilder.Entity<Role>().HasIndex(u => u.SoftDeleted);

            modelBuilder.Entity<Role>().HasIndex(r => new { r.Name, r.SoftDeleted }).IsUnique();

            modelBuilder
                .Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany()
                .UsingEntity<RolePermission>();

            #endregion role

            #region user

            modelBuilder.Entity<User>().HasData(User.Seeds);

            modelBuilder.Entity<User>().HasIndex(u => new { u.Username, u.SoftDeleted }).IsUnique();

            modelBuilder.Entity<User>().HasMany(u => u.Roles).WithMany().UsingEntity<UserRole>();

            modelBuilder.Entity<User>().HasIndex(u => u.SoftDeleted);

            modelBuilder.Entity<User>().OwnsOne(u => u.Settings);

            modelBuilder.Entity<User>().OwnsOne(u => u.Detail);

            #endregion user

            #region permission

            modelBuilder.Entity<Permission>().HasIndex(m => m.Order);

            modelBuilder
                .Entity<Permission>()
                .HasOne(m => m.Parent)
                .WithMany()
                .HasForeignKey(m => m.ParentId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Permission>().HasData(Permission.Seeds);

            modelBuilder.Entity<Permission>().HasIndex(m => m.Code).IsUnique();

            modelBuilder
                .Entity<RolePermission>()
                .HasOne(rm => rm.Permission)
                .WithMany()
                .HasForeignKey(rm => rm.PermissionId);

            modelBuilder
                .Entity<RolePermission>()
                .HasOne(rm => rm.Role)
                .WithMany()
                .HasForeignKey(rm => rm.RoleId);

            modelBuilder.Entity<Permission>().HasData(Permission.Seeds);

            #endregion

            #endregion entities initialize
        }
    }
}
