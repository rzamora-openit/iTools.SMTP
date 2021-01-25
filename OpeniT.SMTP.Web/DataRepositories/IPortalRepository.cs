using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using OpeniT.SMTP.Web.Models;
namespace OpeniT.SMTP.Web.DataRepositories
{
    public interface IPortalRepository
    {
        Task<IdentityResult> CreateUser(ApplicationUser user, string password);
        Task<IdentityResult> CreateAdminUser(ApplicationUser user);

        Task<ApplicationUser> GetUserByUserName(string userName);
        Task<ApplicationUser> GetUserByEmail(string email);
        Task<IdentityResult> UpdateUser(ApplicationUser user);
        Task<IdentityResult> DeleteUser(ApplicationUser user);

        #region contextMethods
        void ResetAll();
        EntityState StateOf(object o);
        TEntity CloneEntry<TEntity>(TEntity source) where TEntity : class;
        Task ReloadEntry<TEntity>(TEntity entity) where TEntity : class;
        Task LoadEntryNavigationEntries<TEntity>(TEntity entity) where TEntity : class;
        Task LoadNestedEntryNavigationEntries<TEntity>(TEntity entity) where TEntity : class;
        Task LoadChildCollection<TEnity>(TEnity parent, string childrenPropertyName) where TEnity : class;
        Task LoadNestedChildren<TEnity>(TEnity parent, string childrenPropertyName) where TEnity : class;
        Task<bool> SaveChangesAsync();
        #endregion contextMethods

        #region GenericMethods
        Task<List<TEntity>> GetAll<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, DataPagination dataPagination = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class;
        Task<TEntity> GetFirst<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class, new();
        Task<TSelect> SelectFirst<TEntity, TSelect>(Expression<Func<TEntity, TSelect>> selectExpression, Expression<Func<TSelect, bool>> filterExpression = null, params DataSort<TSelect, object>[] dataSorts) where TEntity : class;
        Task<int> GetCount<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null) where TEntity : class;
        Task<bool> GetAny<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null) where TEntity : class;
        Task Add<TEntity>(TEntity entity) where TEntity : class;
        void Update<TEntity>(TEntity entity) where TEntity : class;
        void Remove<TEntity>(TEntity entity) where TEntity : class;
        void RemoveRange<TEntity>(IEnumerable<TEntity> entities) where TEntity : class;
        IQueryable<TEntity> GetQueryable<TEntity>(Expression<Func<TEntity, bool>> filterExpression = null, int? includeDepth = null, DataPagination dataPagination = null, params DataSort<TEntity, object>[] dataSorts) where TEntity : class;
        #endregion GenericMethods
    }
}
