using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Threading;
using Liaro.Entities.Security;
using Liaro.Entities;
using System;

namespace Liaro.DataLayer.Context
{
    public class ApplicationDbContext : DbContext
    {
        //DbSets
        public virtual DbSet<User> Users { set; get; }
        public virtual DbSet<Role> Roles { set; get; }
        public virtual DbSet<UserRole> UserRoles { get; set; }
        public virtual DbSet<UserToken> UserTokens { get; set; }
        public virtual DbSet<ShortLink> ShortLinks { get; set; }


        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }


        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            OnBeforeSaving();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        private void OnBeforeSaving()
        {
            CustomChangeTracker<ShortLink>();

        }


        private void CustomChangeTracker<T>() where T : class
        {
            foreach (var entry in ChangeTracker.Entries<T>())
            {
                switch (entry.State)
                {
                    case EntityState.Added:
                        entry.CurrentValues["IsDeleted"] = false;
                        entry.CurrentValues["CreatedOn"] = DateTime.UtcNow;
                        entry.CurrentValues["ModifiedOn"] = DateTime.UtcNow;
                        break;

                    case EntityState.Deleted:
                        entry.State = EntityState.Modified;
                        entry.CurrentValues["IsDeleted"] = true;
                        entry.CurrentValues["ModifiedOn"] = DateTime.UtcNow;
                        break;

                    case EntityState.Modified:
                        entry.State = EntityState.Modified;
                        entry.CurrentValues["ModifiedOn"] = DateTime.UtcNow;
                        break;
                }
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Custom application mappings
            builder.Entity<User>(entity =>
            {
                entity.Property(e => e.Username).HasMaxLength(450).IsRequired();
                entity.HasIndex(e => e.Username).IsUnique();
                entity.Property(e => e.Password).IsRequired();
                entity.Property(e => e.SerialNumber).HasMaxLength(450);
            });

            builder.Entity<Role>(entity =>
            {
                entity.Property(e => e.Name).HasMaxLength(450).IsRequired();
                entity.HasIndex(e => e.Name).IsUnique();
            });

            builder.Entity<UserRole>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
                entity.HasIndex(e => e.UserId);
                entity.HasIndex(e => e.RoleId);
                entity.Property(e => e.UserId);
                entity.Property(e => e.RoleId);
                entity.HasOne(d => d.Role).WithMany(p => p.UserRoles).HasForeignKey(d => d.RoleId);
                entity.HasOne(d => d.User).WithMany(p => p.UserRoles).HasForeignKey(d => d.UserId);
            });

            builder.Entity<UserToken>(entity =>
            {
                entity.HasOne(ut => ut.User)
                        .WithMany(u => u.UserTokens)
                        .HasForeignKey(ut => ut.UserId);

                entity.Property(ut => ut.RefreshTokenIdHash).HasMaxLength(450).IsRequired();
                entity.Property(ut => ut.RefreshTokenIdHashSource).HasMaxLength(450);
            });


            builder.Entity<ShortLink>(entity =>
            {
                // This line will filter soft deleted shortlinks when fetching from db!
                entity.HasQueryFilter(sl => !sl.IsDeleted);
                entity.HasIndex(sl => sl.Source).IsUnique();
            });



        }

    }
}