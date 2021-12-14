
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Query.SqlExpressions;
using Microsoft.EntityFrameworkCore.Query;
using System.Text;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Query.Internal;
using Microsoft.EntityFrameworkCore.Storage;

namespace ManejoPresupuesto.Helpers
{
    public sealed class DBServices<TSource>
    {
        public string QueryString { get; set; }
        public DBServices(string queryString)
        {            
            QueryString = queryString;
        }                
    }
   
    public static class DBServicesHelpers
    {
        /// <summary>
        /// CreateInsertInto
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="clase"></param>
        /// <returns></returns>
        public static string CreateInsertInto<TSource>(this TSource clase)
        {
            var Properties = new List<PropertyInfo>(clase.GetType().GetProperties());
            var TableName = clase.GetType().Name;

            string campos = null;
            string valores = null;

            foreach (PropertyInfo propertyInfo in Properties)
            {
                if (propertyInfo.Name.ToUpper() != "ID")
                {

                    if (campos is not null)
                    {
                        campos += ",";
                    }
                    campos += propertyInfo.Name;

                    if (valores is not null)
                    {
                        valores += ",";
                    }
                    valores += "@" + propertyInfo.Name;
                }
            }

            var query = "INSERT INTO " + TableName + "(" + campos + ") VALUES (" + valores + "); SELECT SCOPE_IDENTITY();";
            return query;
        }

        /// <summary>
        /// CreateUpdate
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="clase"></param>
        /// <returns></returns>
        public static DBServices<TSource> CreateUpdate<TSource>(this TSource clase)
        {

            var Properties = new List<PropertyInfo>(clase.GetType().GetProperties());
            var TableName = clase.GetType().Name;

            string campos = null;
            foreach (PropertyInfo propertyInfo in Properties)
            {
                if (propertyInfo.Name.ToUpper() != "ID")
                {
                    //var valorP = propertyInfo.GetValue(clase, null);
                    if (campos is not null)
                    {
                        campos += Environment.NewLine;
                        campos += "  , ";
                    }
                    campos += propertyInfo.Name + " = @" + propertyInfo.Name;
                }
            }

            StringBuilder sb = new StringBuilder();
            sb.Append($"UPDATE {TableName}");
            sb.Append(Environment.NewLine);
            sb.Append($"SET " + campos);

            return new DBServices<TSource>(sb.ToString());
        }

        /// <summary>
        /// CreateDelete
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <param name="clase"></param>
        /// <returns></returns>
        public static DBServices<TSource> CreateDelete<TSource>(this TSource clase)
        {                             
            return new DBServices<TSource>($"DELETE [dbo].[{clase.GetType().Name}]");
        }

        /// <summary>
        /// Where
        /// </summary>
        /// <param name="property"></param>
        /// <exception cref="Exception"></exception>
        public static DBServices<TSource> WhereCondition<TSource>(this DBServices<TSource> services, Expression<Func<TSource, int>> property)
        {
            if (services.QueryString.Contains("INSERT") || services.QueryString is null)
            {
                throw new Exception("No es posible agregar un where a una sentencia Insert");
            }

            string nameProperties = GetPropertyName(property);

            StringBuilder sb = new();
            sb.Append(services.QueryString);
            sb.Append(Environment.NewLine);
            sb.Append("WHERE " + nameProperties + " = @" + nameProperties);

            services.QueryString = sb.ToString();
            return services;
        }

        /// <summary>
        /// AddGeneric
        /// </summary>
        /// <typeparam name="TSource"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="services"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static DBServices<TSource> AddCondition<TSource, TResult>(this DBServices<TSource> services, Expression<Func<TSource, TResult>> expression)
        {
            if (services.QueryString.Contains("INSERT") || services.QueryString is null)
            {
                throw new Exception("No es posible agregar un and a una sentencia Insert");
            }

            StringBuilder sb = new();
            sb.AppendLine(services.QueryString);

            string nameProperties = GetPropertyName(expression);
            sb.Append(Environment.NewLine);
            sb.Append("AND " + nameProperties + " = @" + nameProperties);

            services.QueryString = sb.ToString();
            return services;
        }

        /// <summary>
        /// GetPropertyName
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static string GetPropertyName<TSource, TResult>(Expression<Func<TSource, TResult>> action)
        {
            var memberExpression = (MemberExpression)action.Body;
            //var propertyInfo = (PropertyInfo)memberExpression.Member;
            return memberExpression.Member.Name;
        }
    }
}

