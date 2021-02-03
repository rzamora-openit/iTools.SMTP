using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using OpeniT.SMTP.Web.Models;
using Newtonsoft.Json;
using OpeniT.SMTP.Web.Pages.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Template;
using Microsoft.AspNetCore.Http;

namespace OpeniT.SMTP.Web.Methods
{
    public static class ExtensionMethods
    {
        public static IQueryable<T> Include<T>(this IQueryable<T> source, IEnumerable<string> navigationPropertyPaths)
            where T : class
        {
            return navigationPropertyPaths.Aggregate(source, (query, path) => query.Include(path));
        }

        public static IQueryable<TEntity> Include<TEntity>(this IQueryable<TEntity> source, IEnumerable<Expression<Func<TEntity, object>>> navigationPropertyPaths)
            where TEntity : class
        {
            return navigationPropertyPaths.Aggregate(source, (query, path) => query.Include(path));
        }

        public static IEnumerable<string> GetIncludePaths(this DbContext context, Type clrEntityType, int maxDepth = int.MaxValue)
        {
            if (maxDepth < 0) throw new ArgumentOutOfRangeException(nameof(maxDepth));
            var entityType = context.Model.FindEntityType(clrEntityType);
            var includedNavigations = new HashSet<INavigation>();
            var stack = new Stack<IEnumerator<INavigation>>();
            while (true)
            {
                var entityNavigations = new List<INavigation>();
                if (stack.Count <= maxDepth)
                {
                    var x = entityType.GetDeclaredNavigations();
                    foreach (var navigation in entityType.GetNavigations())
                    {
                        if (includedNavigations.Add(navigation))
                            entityNavigations.Add(navigation);
                    }
                }
                if (entityNavigations.Count == 0)
                {
                    if (stack.Count > 0)
                        yield return string.Join(".", stack.Reverse().Select(e => e.Current.Name));
                }
                else
                {
                    foreach (var navigation in entityNavigations)
                    {
                        var inverseNavigation = navigation.Inverse;
                        if (inverseNavigation != null)
                            includedNavigations.Add(inverseNavigation);
                    }
                    stack.Push(entityNavigations.GetEnumerator());
                }
                while (stack.Count > 0 && !stack.Peek().MoveNext())
                    stack.Pop();
                if (stack.Count == 0) break;
                entityType = stack.Peek().Current.TargetEntityType;
            }
        }

        public static void Reload(this CollectionEntry source)
        {
            if (source.CurrentValue != null)
            {
                foreach (var item in source.CurrentValue)
                    source.EntityEntry.Context.Entry(item).State = EntityState.Detached;
                source.CurrentValue = null;
            }
            source.IsLoaded = false;
            source.Load();
        }

        public static async Task ReloadAsync(this CollectionEntry source)
        {
            if (source.CurrentValue != null)
            {
                foreach (var item in source.CurrentValue)
                    source.EntityEntry.Context.Entry(item).State = EntityState.Detached;
                source.CurrentValue = null;
            }
            source.IsLoaded = false;
            await source.LoadAsync();
        }

        public static void Reload(this ReferenceEntry source)
        {
            if (source.CurrentValue != null)
            {
                source.EntityEntry.Context.Entry(source.CurrentValue).State = EntityState.Detached;
                source.CurrentValue = null;
            }
            source.IsLoaded = false;
            source.Load();
        }

        public static async Task ReloadAsync(this ReferenceEntry source)
        {
            if (source.CurrentValue != null)
            {
                source.EntityEntry.Context.Entry(source.CurrentValue).State = EntityState.Detached;
                source.CurrentValue = null;
            }
            source.IsLoaded = false;
            await source.LoadAsync();
        }

        public static IEnumerable<T> SearchCollection<T>(this IEnumerable<T> items, string searchValue, IEnumerable<Func<T, string>> searchFuncs)
        {
            if (items == null || string.IsNullOrEmpty(searchValue) || !items.Any() || !searchFuncs.Any())
            {
                return items;
            }

            var filteredCollection = new List<T>();
            foreach (var item in items)
            {
                var values = new List<string>();
                foreach (var searchFunc in searchFuncs)
                {
                    string fieldValue = searchFunc.Invoke(item);

                    values.Add(fieldValue);
                }

                if (!values.Any())
                {
                    continue;
                }

                if (values.Any(x => x != null && x.ToLower().Contains(searchValue.ToLower())))
                {
                    filteredCollection.Add(item);
                }
            }

            return filteredCollection;
        }

        public static bool TryGetQueryString<T>(this NavigationManager navManager, string key, out T value)
        {
            var uri = navManager.ToAbsoluteUri(navManager.Uri);

            if (QueryHelpers.ParseQuery(uri.Query).TryGetValue(key, out var valueFromQueryString))
            {
                if (typeof(T) == typeof(int) && int.TryParse(valueFromQueryString, out var valueAsInt))
                {
                    value = (T)(object)valueAsInt;
                    return true;
                }

                if (typeof(T) == typeof(string))
                {
                    value = (T)(object)valueFromQueryString.ToString();
                    return true;
                }

                if (typeof(T) == typeof(decimal) && decimal.TryParse(valueFromQueryString, out var valueAsDecimal))
                {
                    value = (T)(object)valueAsDecimal;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public static string GetBaseRelativePath(this NavigationManager navigationManager)
        {
            return "/" + navigationManager.ToBaseRelativePath(navigationManager.Uri).Split("?").FirstOrDefault() ?? string.Empty;
        }

        public static string ToAbsoluteUriString(this NavigationManager navigationManager, string relativeUri)
        {
            return navigationManager.ToAbsoluteUri(relativeUri).AbsoluteUri;
        }

        public static bool RouteTemplateMatch(this NavigationManager navigationManager, string routeTemplate)
        {
            var template = TemplateParser.Parse(routeTemplate);
            var routeValues = new RouteValueDictionary();
            foreach (var parameter in template.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    routeValues.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            var requestPath = navigationManager.GetBaseRelativePath();
            var template2 = TemplateParser.Parse(requestPath);
            var routeValues2 = new RouteValueDictionary();
            foreach (var parameter in template2.Parameters)
            {
                if (parameter.DefaultValue != null)
                {
                    routeValues2.Add(parameter.Name, parameter.DefaultValue);
                }
            }

            var matcher = new TemplateMatcher(template, routeValues);
            return matcher.TryMatch(new PathString(requestPath), routeValues2);
        }

        public static object GetObjectValue(this object obj, string fieldNames)
        {
            object currentObject = obj;
            string[] _fieldNames = fieldNames.Split(".");

            foreach (string fieldName in _fieldNames)
            {
                if (currentObject == null)
                    break;

                Type curentRecordType = currentObject.GetType();
                PropertyInfo property = curentRecordType.GetProperty(fieldName);

                if (property != null)
                {
                    if (property.CanRead && property.GetGetMethod(true).IsPublic)
                    {
                        currentObject = property.GetValue(currentObject, null);
                    }
                }
                else
                {
                    break;
                }
            }

            return currentObject;
        }

        public static string GetUniqueName(this MethodInfo mi)
        {
            string signatureString = string.Join(",", mi.GetParameters().Select(p => p.ParameterType.Name).ToArray());
            string returnTypeName = mi.ReturnType.Name;

            if (mi.IsGenericMethod)
            {
                string typeParamsString = string.Join(",", mi.GetGenericArguments().Select(g => g.AssemblyQualifiedName).ToArray());


                // returns a string like this: "Assembly.YourSolution.YourProject.YourClass:YourMethod(Param1TypeName,...,ParamNTypeName):ReturnTypeName
                return string.Format("{0}:{1}<{2}>({3}):{4}", mi.DeclaringType.AssemblyQualifiedName, mi.Name, typeParamsString, signatureString, returnTypeName);
            }

            return string.Format("{0}:{1}({2}):{3}", mi.DeclaringType.AssemblyQualifiedName, mi.Name, signatureString, returnTypeName);
        }

        public static string ToSqlDateString(this DateTime? dateTime)
        {
            return dateTime?.ToString("yyyy-MM-dd");
        }

        public static string ToSqlDateString(this DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd");
        }

        public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dict, TKey key) where TValue : new()
        {
            TValue val;

            if (!dict.TryGetValue(key, out val))
            {
                val = new TValue();
                dict.Add(key, val);
            }

            return val;
        }

        public static TValue InitializeIfNull<TValue>(this TValue obj) where TValue : new()
        {
            obj = obj ?? new TValue();
            return obj;
        }

        public static async Task<T> Enqueue<T>(this SemaphoreSlim semaphoreSlim, Func<Task<T>> task)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return await task();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static async Task Enqueue(this SemaphoreSlim semaphoreSlim, Func<Task> task)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                await task();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static async Task<T> Enqueue<T>(this SemaphoreSlim semaphoreSlim, Func<T> func)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                return func();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static async Task Enqueue(this SemaphoreSlim semaphoreSlim, Action action)
        {
            await semaphoreSlim.WaitAsync();
            try
            {
                action();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }

        public static FieldIdentifier Field<TField>(this EditContext editContext, Expression<Func<TField>> accessor)
		{
            return FieldIdentifier.Create(accessor);
        }

        public static void NotifyFieldChanged<TField>(this EditContext editContext, Expression<Func<TField>> accessor)
        {
            editContext.NotifyFieldChanged(editContext.Field(accessor));
        }
    }
}