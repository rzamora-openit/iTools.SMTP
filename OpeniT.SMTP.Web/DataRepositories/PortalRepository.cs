using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Logging;
using OpeniT.SMTP.Web.Models;
using OpeniT.SMTP.Web.Methods;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.ObjectModel;

namespace OpeniT.SMTP.Web.DataRepositories
{
    public class PortalRepository : IPortalRepository
    {
        private readonly DataContext context;
        private readonly ILogger<DataContext> logger;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public PortalRepository(UserManager<ApplicationUser> userManager,
                                SignInManager<ApplicationUser> signInManager,
                                DataContext context,
                                ILogger<DataContext> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
            this.logger = logger;
        }

        public async Task<IdentityResult> CreateUser(ApplicationUser user, string password)
        {
            return await this.userManager.CreateAsync(user, password);            
        }

        public async Task<IdentityResult> CreateAdminUser(ApplicationUser user)
        {
            var save = await this.userManager.CreateAsync(user);
            return save;
        }

        public async Task<ApplicationUser> GetUserByUserName(string userName)
        {
            return await this.userManager.Users.Where(u => u.UserName == userName)
                .FirstOrDefaultAsync();
        }

        public async Task<ApplicationUser> GetUserByEmail(string email)
        {
            return await this.userManager.Users.Where(u => u.Email == email)
                .FirstOrDefaultAsync();
        }

        public async Task<IdentityResult> UpdateUser(ApplicationUser user)
        {
            return await this.userManager.UpdateAsync(user);
        }

        public async Task<IdentityResult> DeleteUser(ApplicationUser user)
        {
            return await this.userManager.DeleteAsync(user);
        }

        #region contextMethods
        public void ResetAll()
        {
            var entries = this.context.ChangeTracker
                                 .Entries()
                                 .Where(e => e.State != EntityState.Unchanged)
                                 .ToArray();

            foreach (var entry in entries)
            {
                switch (entry.State)
                {
                    case EntityState.Modified:
                        entry.State = EntityState.Unchanged;
                        break;
                    case EntityState.Added:
                        entry.State = EntityState.Detached;
                        break;
                    case EntityState.Deleted:
                        entry.Reload();
                        break;
                }
            }
        }
        public EntityState StateOf(object o)
        {
            try
            {
                return this.context.Entry(o).State;
            }
            catch (Exception e)
            {
                return EntityState.Detached;
            };
        }
        public TEntity CloneEntry<TEntity>(TEntity source) where TEntity : class
        {
            var destination = (TEntity)Activator.CreateInstance(source.GetType());
            if (source != null)
            {
                var sourceEntry = this.context.Entry(source);
                var destinationEntry = this.context.Entry(destination);

                destinationEntry.CurrentValues.SetValues(sourceEntry.CurrentValues.Clone());
                var keyProperties = this.context.Model.FindEntityType(destination.GetType()).FindPrimaryKey().Properties;
                foreach (var keyProperty in keyProperties)
				{
                    if (keyProperty.ClrType.IsValueType)
					{
                        destinationEntry.Property(keyProperty.Name).CurrentValue = Activator.CreateInstance(keyProperty.ClrType);
                    }
                    else
                    {
                        destinationEntry.Property(keyProperty.Name).CurrentValue = null;
                    }
                }

                foreach (var referenceEntry in sourceEntry.References)
                {
                    if (referenceEntry.CurrentValue != null)
                    {
                        destinationEntry.Reference(referenceEntry.Metadata.Name).CurrentValue = this.CloneEntry(referenceEntry.CurrentValue);
                    }
                }

                foreach (var collectionEntry in sourceEntry.Collections)
                {
                    if (collectionEntry.CurrentValue != null)
                    {
                        var copyValuesType = typeof(List<>).MakeGenericType(collectionEntry.Metadata.PropertyInfo.PropertyType.GetGenericArguments()[0]);
                        var copyValues = (IList)Activator.CreateInstance(copyValuesType);
                        foreach (var entry in collectionEntry.CurrentValue)
                        {
                            object copyValue = null;
                            copyValue = this.CloneEntry(entry);

                            copyValues.Add(copyValue);
                        }

                        destinationEntry.Collection(collectionEntry.Metadata.Name).CurrentValue = copyValues;
                    }
                }
            }

            return destination;
        }
        public async Task ReloadEntry<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity != null)
            {
                var entry = this.context.Entry(entity);
                if (entry.State != EntityState.Detached)
                {
                    foreach (var collectionEntry in entry.Collections.Where(c => c.IsModified))
                    {
                        await collectionEntry.ReloadAsync();
                    }

                    if (entry.State == EntityState.Modified)
                    {
                        await entry.ReloadAsync();
                    }
                }
            }
        }
        public async Task LoadEntryNavigationEntries<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity != null)
            {
                var entry = this.context.Entry(entity);
                if (entry.State != EntityState.Detached)
                {
                    foreach (var referenceEntry in entry.References.Where(r => !r.IsLoaded))
                    {
                        await referenceEntry.LoadAsync();
                    }

                    foreach (var collectionEntry in entry.Collections.Where(c => !c.IsLoaded))
                    {
                        await collectionEntry.LoadAsync();
                    }
                }
            }
        }
        public async Task LoadNestedEntryNavigationEntries<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity != null)
            {
                var entry = this.context.Entry(entity);
                if (entry.State != EntityState.Detached)
                {
                    foreach (var referenceEntry in entry.References.Where(r => !r.IsLoaded))
                    {
                        await referenceEntry.LoadAsync();

                        if (referenceEntry.CurrentValue != null)
                        {
                            await this.LoadNestedEntryNavigationEntries(referenceEntry.CurrentValue);
                        }
                    }

                    foreach (var collectionEntry in entry.Collections.Where(c => !c.IsLoaded))
                    {
                        await collectionEntry.LoadAsync();

                        foreach (var value in collectionEntry.CurrentValue)
                        {
                            if (value != null)
                            {
                                await this.LoadNestedEntryNavigationEntries(value);
                            }
                        }
                    }
                }
            }
        }
        public async Task LoadChildCollection<TEnity>(TEnity parent, string childrenPropertyName) where TEnity : class
        {
            var collectionEntry = this.context.Entry(parent).Collection(childrenPropertyName);
            if (!collectionEntry.IsLoaded)
            {
                await collectionEntry.LoadAsync();
            }
        }
        public async Task LoadNestedChildren<TEnity>(TEnity parent, string childrenPropertyName) where TEnity : class
        {
            var collectionEntry = this.context.Entry(parent).Collection(childrenPropertyName);
            if (!collectionEntry.IsLoaded)
            {
                await collectionEntry.LoadAsync();
            }

            if (collectionEntry.CurrentValue != null)
            {
                foreach (var value in collectionEntry.CurrentValue)
                {
                    if (value != null)
                    {
                        await this.LoadNestedChildren(value, childrenPropertyName);
                    }
                }
            }
        }
        public async Task<bool> SaveChangesAsync()
        {
            var added = this.context.ChangeTracker.Entries().Where(e => e.State == EntityState.Added);
            foreach (var item in added)
            {
                if (item.Entity.GetType().BaseType == typeof(BaseClass))
                {
                    ((BaseClass)item.Entity).DateCreated = DateTime.UtcNow;
					((BaseClass)item.Entity).CreatedById = signInManager?.Context?.User?.Identity?.Name;
				}
                else if (item.Entity.GetType().BaseType == typeof(BaseCommon))
                {
                    ((BaseCommon)item.Entity).DateCreated = DateTime.UtcNow;
					((BaseCommon)item.Entity).CreatedById = signInManager?.Context?.User?.Identity?.Name;
                }
                else if (item.Entity.GetType().BaseType == typeof(BaseEnum))
                {
                    ((BaseEnum)item.Entity).DateCreated = DateTime.UtcNow;
					((BaseEnum)item.Entity).CreatedById = signInManager?.Context?.User?.Identity?.Name;
                }
            }

            var modified = this.context.ChangeTracker.Entries().Where(e => e.State == EntityState.Modified);
            foreach (var item in modified)
            {
                if (item.Entity.GetType().BaseType == typeof(BaseClass))
                {
                    ((BaseClass)item.Entity).DateUpdated = DateTime.UtcNow;
					((BaseClass)item.Entity).LastUpdatedById = signInManager?.Context?.User?.Identity?.Name;
                }
                else if (item.Entity.GetType().BaseType == typeof(BaseCommon))
                {
                    ((BaseCommon)item.Entity).DateUpdated = DateTime.UtcNow;
					((BaseCommon)item.Entity).LastUpdatedById = signInManager?.Context?.User?.Identity?.Name;
                }
                else if (item.Entity.GetType().BaseType == typeof(BaseEnum))
                {
                    ((BaseEnum)item.Entity).DateUpdated = DateTime.UtcNow;
					((BaseEnum)item.Entity).LastUpdatedById = signInManager?.Context?.User?.Identity?.Name;
                }
            }
            //var deleted = this.context.ChangeTracker.Entries().Where(e => e.State == EntityState.Deleted);

            var results = await this.context.SaveChangesAsync();

            return results > 0;
        }
        #endregion contextMethods

        #region GenericMethods
        public async Task<List<TEntity>> GetAll<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, DataPagination dataPagination = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class
        {
            var includePaths = includeDepth.HasValue ?
                this.context.GetIncludePaths(typeof(TEntity), includeDepth.Value - 1).Distinct() :
                null;

            IQueryable<TEntity> entities = includePaths != null && includePaths.Any() ?
                this.context.Set<TEntity>().Include(includePaths) :
                this.context.Set<TEntity>();

            if (dataSorts != null && dataSorts.Any(ds => ds != null))
            {
                IOrderedQueryable<TEntity> orderedEntities = null;
                entities = dataSorts.Aggregate(entities, (current, dataSort) => {
                    if (dataSort == dataSorts.First())
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? entities.OrderBy(dataSort.OrderExpression) : entities.OrderByDescending(dataSort.OrderExpression);
                    }
                    else
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? orderedEntities.ThenBy(dataSort.OrderExpression) : orderedEntities.ThenByDescending(dataSort.OrderExpression);
                    }

                    return orderedEntities;
                });
            }

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            if (dataPagination != null)
            {
                entities = entities
                    .Skip(dataPagination.PageIndex * dataPagination.PageSize)
                    .Take(dataPagination.PageSize);
            }

            return await entities.ToListAsync();
        }
        public async Task<TEntity> GetFirst<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class, new()
        {
            var includePaths = includeDepth.HasValue ?
                this.context.GetIncludePaths(typeof(TEntity), includeDepth.Value - 1).Distinct() :
                null;

            IQueryable<TEntity> entities = includePaths != null && includePaths.Any() ?
                this.context.Set<TEntity>().Include(includePaths) :
                this.context.Set<TEntity>();

            if (dataSorts != null && dataSorts.Any(ds => ds != null))
            {
                IOrderedQueryable<TEntity> orderedEntities = null;
                entities = dataSorts.Aggregate(entities, (current, dataSort) => {
                    if (dataSort == dataSorts.First())
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? entities.OrderBy(dataSort.OrderExpression) : entities.OrderByDescending(dataSort.OrderExpression);
                    }
                    else
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? orderedEntities.ThenBy(dataSort.OrderExpression) : orderedEntities.ThenByDescending(dataSort.OrderExpression);
                    }

                    return orderedEntities;
                });
            }

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            return await entities.FirstOrDefaultAsync();
        }
        public async Task<TSelect> SelectFirst<TEntity, TSelect>(Expression<Func<TEntity, TSelect>> selectExpression, Expression <Func<TSelect, bool>> filterExpression = null, params DataSort<TSelect, object>[] dataSorts) where TEntity : class
        {
            IQueryable<TSelect> entities = this.context.Set<TEntity>().Select(selectExpression);

            if (dataSorts != null && dataSorts.Any(ds => ds != null))
            {
                IOrderedQueryable<TSelect> orderedEntities = null;
                entities = dataSorts.Aggregate(entities, (current, dataSort) => {
                    if (dataSort == dataSorts.First())
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? entities.OrderBy(dataSort.OrderExpression) : entities.OrderByDescending(dataSort.OrderExpression);
                    }
                    else
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? orderedEntities.ThenBy(dataSort.OrderExpression) : orderedEntities.ThenByDescending(dataSort.OrderExpression);
                    }

                    return orderedEntities;
                });
            }

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            return await entities.FirstOrDefaultAsync();
        }
        public async Task<int> GetCount<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null) where TEntity : class
        {
            IQueryable<TEntity> entities = this.context.Set<TEntity>();

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            return await entities.CountAsync();
        }
        public async Task<bool> GetAny<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null) where TEntity : class
        {
            IQueryable<TEntity> entities = this.context.Set<TEntity>();

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            return await entities.AnyAsync();
        }
        public async Task Add<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null) return;

            await this.context.Set<TEntity>().AddAsync(entity);
        }
        public void Update<TEntity>(TEntity entity) where TEntity : class
        {
            if (entity == null) return;

            this.context.Set<TEntity>().Update(entity);
        }
        public void Remove<TEntity>(TEntity entity) where TEntity : class
		{
            if (entity == null) return;

            this.context.Set<TEntity>().Remove(entity);
        }
        public void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class
        {
            if (entities?.Where(e => e != null)?.Any() != true) return;

            this.context.Set<TEntity>().RemoveRange(entities);
        }
        public IQueryable<TEntity> GetQueryable<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, DataPagination dataPagination = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class
        {
            var includePaths = includeDepth.HasValue ?
                this.context.GetIncludePaths(typeof(TEntity), includeDepth.Value - 1).Distinct() :
                null;

            IQueryable<TEntity> entities = includePaths != null && includePaths.Any() ?
                this.context.Set<TEntity>().Include(includePaths) :
                this.context.Set<TEntity>();

            if (dataSorts != null && dataSorts.Any(ds => ds != null))
            {
                IOrderedQueryable<TEntity> orderedEntities = null;
                entities = dataSorts.Aggregate(entities, (current, dataSort) => {
                    if (dataSort == dataSorts.First())
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? entities.OrderBy(dataSort.OrderExpression) : entities.OrderByDescending(dataSort.OrderExpression);
                    }
                    else
                    {
                        orderedEntities = dataSort.SortDirection == SortDirection.ASC ? orderedEntities.ThenBy(dataSort.OrderExpression) : orderedEntities.ThenByDescending(dataSort.OrderExpression);
                    }

                    return orderedEntities;
                });
            }

            if (filterExpression != null)
            {
                entities = entities.Where(filterExpression);
            }

            if (dataPagination != null)
            {
                entities = entities
                    .Skip(dataPagination.PageIndex * dataPagination.PageSize)
                    .Take(dataPagination.PageSize);
            }

            return entities;
        }
        #endregion GenericMethods
    }
}
