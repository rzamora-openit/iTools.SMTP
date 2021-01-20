using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using OpeniT.SMTP.Web.Models;

namespace OpeniT.SMTP.Web.DataRepositories
{
    public partial class DataContext : IdentityDbContext<ApplicationUser>
    {

        public DataContext(DbContextOptions<DataContext> options)
            : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //#region IdentityFramework
            //builder.Entity<ApplicationUser>(entity => entity.Property(m => m.Id).HasMaxLength(85));
            //builder.Entity<ApplicationUser>(entity => entity.Property(m => m.NormalizedEmail).HasMaxLength(85));
            //builder.Entity<ApplicationUser>(entity => entity.Property(m => m.NormalizedUserName).HasMaxLength(85));

            //builder.Entity<IdentityRole>(entity => entity.Property(m => m.Id).HasMaxLength(85));
            //builder.Entity<IdentityRole>(entity => entity.Property(m => m.NormalizedName).HasMaxLength(85));

            //builder.Entity<IdentityUserLogin<string>>(entity => entity.Property(m => m.LoginProvider).HasMaxLength(85));
            //builder.Entity<IdentityUserLogin<string>>(entity => entity.Property(m => m.ProviderKey).HasMaxLength(85));
            //builder.Entity<IdentityUserLogin<string>>(entity => entity.Property(m => m.UserId).HasMaxLength(85));
            //builder.Entity<IdentityUserRole<string>>(entity => entity.Property(m => m.UserId).HasMaxLength(85));

            //builder.Entity<IdentityUserRole<string>>(entity => entity.Property(m => m.RoleId).HasMaxLength(85));

            //builder.Entity<IdentityUserToken<string>>(entity => entity.Property(m => m.UserId).HasMaxLength(85));
            //builder.Entity<IdentityUserToken<string>>(entity => entity.Property(m => m.LoginProvider).HasMaxLength(85));
            //builder.Entity<IdentityUserToken<string>>(entity => entity.Property(m => m.Name).HasMaxLength(85));

            //builder.Entity<IdentityUserClaim<string>>(entity => entity.Property(m => m.Id).HasMaxLength(85));
            //builder.Entity<IdentityUserClaim<string>>(entity => entity.Property(m => m.UserId).HasMaxLength(85));
            //builder.Entity<IdentityRoleClaim<string>>(entity => entity.Property(m => m.Id).HasMaxLength(85));
            //builder.Entity<IdentityRoleClaim<string>>(entity => entity.Property(m => m.RoleId).HasMaxLength(85));
            //#endregion IdentityFramework

            builder.Entity<SmtpMail>().HasIndex(e => e.Guid).IsUnique();
        }

        #region Smtp
        public virtual DbSet<SmtpMail> SmtpMails { get; set; }
        public virtual DbSet<SmtpMailAddress> SmtpMailAddresses { get; set; }

        #endregion Smtp

        private void BeforeSavingChanges()
        {
            var entries = this.ChangeTracker.Entries();
            foreach (var entry in entries)
            {
                var entityType = entry.Entity.GetType();
                if (entityType.GetInterfaces().Contains(typeof(IBaseBeforeSavingChanges)))
                {
                    var baseInterface = (IBaseBeforeSavingChanges)entry.Entity;
                    baseInterface.BeforeSavingChanges();
                }
            }
        }

        #region SaveChangesOverrides
        public override int SaveChanges()
        {
            this.BeforeSavingChanges();

            return base.SaveChanges();
        }

        public override int SaveChanges(bool acceptAllChangesOnSuccess)
        {
            this.BeforeSavingChanges();

            return base.SaveChanges(acceptAllChangesOnSuccess);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default(CancellationToken))
        {
            this.BeforeSavingChanges();

            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            this.BeforeSavingChanges();

            return base.SaveChangesAsync(cancellationToken);
        }
        #endregion SaveChangesOverrides
    }
}
