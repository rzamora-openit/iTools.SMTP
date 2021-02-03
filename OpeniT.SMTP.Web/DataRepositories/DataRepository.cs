﻿using Microsoft.AspNetCore.Identity;
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
using System.Threading;

namespace OpeniT.SMTP.Web.DataRepositories
{
    public class DataRepository : IDataRepository
    {
        private readonly DataContext context;
        private readonly ILogger<DataContext> logger;

        private readonly UserManager<ApplicationUser> userManager;
        private readonly SignInManager<ApplicationUser> signInManager;

        public DataRepository(UserManager<ApplicationUser> userManager,
                                SignInManager<ApplicationUser> signInManager,
                                DataContext context,
                                ILogger<DataContext> logger)
        {
            this.userManager = userManager;
            this.signInManager = signInManager;
            this.context = context;
            this.logger = logger;
        }

        private SemaphoreSlim singleTaskQueue = new SemaphoreSlim(1);

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
            return await singleTaskQueue.Enqueue(() => this.userManager.Users.Where(u => u.UserName == userName).FirstOrDefaultAsync());
        }

        public async Task<ApplicationUser> GetUserByEmail(string email)
        {
            return await singleTaskQueue.Enqueue(() => this.userManager.Users.Where(u => u.Email == email).FirstOrDefaultAsync());
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
        public TEntity CloneEntry<TEntity>(TEntity source, TEntity destination = null, bool resetIds = false, bool deepClone = false) where TEntity : class
        {
            if (source == null)
            {
                return null;
            }

            var sourceEntry = this.context.Entry(source);

            var dest = destination ?? (TEntity)Activator.CreateInstance(source.GetType());
            var destinationEntry = this.context.Entry(dest);

            destinationEntry.CurrentValues.SetValues(sourceEntry.CurrentValues.Clone());

            if (resetIds)
            {
                var keyProperties = this.context.Model.FindEntityType(dest.GetType()).FindPrimaryKey().Properties;
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
            }

            if (deepClone)
            {
                foreach (var referenceEntry in sourceEntry.References)
                {
                    if (referenceEntry.CurrentValue != null)
                    {
                        destinationEntry.Reference(referenceEntry.Metadata.Name).CurrentValue = this.CloneEntry(source: referenceEntry.CurrentValue, resetIds: resetIds, deepClone: deepClone);
                    }
                }

                foreach (var collectionEntry in sourceEntry.Collections)
                {
                    if (collectionEntry.CurrentValue != null)
                    {
                        var cloneValuesType = typeof(List<>).MakeGenericType(collectionEntry.Metadata.PropertyInfo.PropertyType.GetGenericArguments()[0]);
                        var cloneValues = (IList)Activator.CreateInstance(cloneValuesType);
                        foreach (var entry in collectionEntry.CurrentValue)
                        {
                            object cloneValue = this.CloneEntry(source: entry, resetIds: resetIds, deepClone: deepClone);

                            cloneValues.Add(cloneValue);
                        }

                        destinationEntry.Collection(collectionEntry.Metadata.Name).CurrentValue = cloneValues;
                    }
                }
            }
            else
            {
                foreach (var referenceEntry in sourceEntry.References)
                {
                    destinationEntry.Reference(referenceEntry.Metadata.Name).CurrentValue = referenceEntry.CurrentValue;
                }

                foreach (var collectionEntry in sourceEntry.Collections)
                {
                    if (collectionEntry.CurrentValue != null)
                    {
                        var cloneValuesType = typeof(List<>).MakeGenericType(collectionEntry.Metadata.PropertyInfo.PropertyType.GetGenericArguments()[0]);
                        var cloneValues = (IList)Activator.CreateInstance(cloneValuesType);
                        foreach (var entry in collectionEntry.CurrentValue)
                        {
                            cloneValues.Add(entry);
                        }

                        destinationEntry.Collection(collectionEntry.Metadata.Name).CurrentValue = cloneValues;
                    }
                }
            }

            return dest;
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
                        await singleTaskQueue.Enqueue(() => collectionEntry.ReloadAsync());
                    }

                    if (entry.State == EntityState.Modified)
                    {
                        await singleTaskQueue.Enqueue(() => entry.ReloadAsync());
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
                        await singleTaskQueue.Enqueue(() => referenceEntry.LoadAsync());
                    }

                    foreach (var collectionEntry in entry.Collections.Where(c => !c.IsLoaded))
                    {
                        await singleTaskQueue.Enqueue(() => collectionEntry.LoadAsync());
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
                        await singleTaskQueue.Enqueue(() => referenceEntry.LoadAsync());

                        if (referenceEntry.CurrentValue != null)
                        {
                            await this.LoadNestedEntryNavigationEntries(referenceEntry.CurrentValue);
                        }
                    }

                    foreach (var collectionEntry in entry.Collections.Where(c => !c.IsLoaded))
                    {
                        await singleTaskQueue.Enqueue(() => collectionEntry.LoadAsync());

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
                await singleTaskQueue.Enqueue(() => collectionEntry.LoadAsync());
            }
        }
        public async Task LoadNestedChildren<TEnity>(TEnity parent, string childrenPropertyName) where TEnity : class
        {
            var collectionEntry = this.context.Entry(parent).Collection(childrenPropertyName);
            if (!collectionEntry.IsLoaded)
            {
                await singleTaskQueue.Enqueue(() => collectionEntry.LoadAsync());
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

            var results = await singleTaskQueue.Enqueue(() => this.context.SaveChangesAsync());

            return results > 0;
        }
        #endregion contextMethods

        #region GenericMethods
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
        public async Task<List<TEntity>> GetAll<TEntity>(IQueryable<TEntity> query, CancellationToken cancellationToken = default) where TEntity : class
        {
            return await singleTaskQueue.Enqueue(() => query.ToListAsync(cancellationToken));
        }
        public async Task<List<TEntity>> GetAll<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, DataPagination dataPagination = null, CancellationToken cancellationToken = default, params DataSort<TEntity, object>[] dataSorts) where TEntity : class
        {
            IQueryable<TEntity> entitiesQuery = this.GetQueryable(filterExpression, includeDepth, dataPagination, dataSorts: dataSorts);
            return await singleTaskQueue.Enqueue(() => this.GetAll(entitiesQuery, cancellationToken));
        }
        public async Task<TEntity> GetFirst<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, CancellationToken cancellationToken = default, params DataSort<TEntity, object>[] dataSorts) where TEntity : class, new()
        {
            IQueryable<TEntity> entities = this.GetQueryable(filterExpression, includeDepth, dataSorts: dataSorts);
            return await singleTaskQueue.Enqueue(() => entities.FirstOrDefaultAsync(cancellationToken));
        }
        public async Task<int> GetCount<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            IQueryable<TEntity> entities = this.GetQueryable(filterExpression);
            return await singleTaskQueue.Enqueue(() => entities.CountAsync(cancellationToken));
        }
        public async Task<bool> GetAny<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, CancellationToken cancellationToken = default) where TEntity : class
        {
            IQueryable<TEntity> entities = this.GetQueryable(filterExpression);
            return await singleTaskQueue.Enqueue(() => entities.AnyAsync(cancellationToken));
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
        #endregion GenericMethods
    }
}
