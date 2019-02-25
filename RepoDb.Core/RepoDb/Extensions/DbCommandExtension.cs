﻿using RepoDb.Attributes;
using System;
using System.Collections.Generic;
using System.Data;
using System.Dynamic;
using System.Linq;
using System.Reflection;

namespace RepoDb.Extensions
{
    /// <summary>
    /// Contains the extension methods for <see cref="IDbCommand"/> object.
    /// </summary>
    public static class DbCommandExtension
    {
        /// <summary>
        /// Creates a parameter for a command object.
        /// </summary>
        /// <param name="command">The command object instance to be used.</param>
        /// <param name="name">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="dbType">The database type of the parameter.</param>
        /// <returns>An instance of the newly created parameter object.</returns>
        public static IDbDataParameter CreateParameter(this IDbCommand command, string name, object value, DbType? dbType = null)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value ?? DBNull.Value;
            if (dbType != null)
            {
                parameter.DbType = dbType.Value;
            }
            return parameter;
        }

        /// <summary>
        /// Creates a parameter for a command object.
        /// </summary>
        /// <param name="command">The command object instance to be used.</param>
        /// <param name="parameterName">The name of the parameter to be replaced.</param>
        /// <param name="values">The array of the values.</param>
        internal static void CreateParametersFromArray(this IDbCommand command, string parameterName, IEnumerable<object> values)
        {
            if (values != null)
            {
                for (var i = 0; i < values.Count(); i++)
                {
                    var parameter = command.CreateParameter(string.Concat(parameterName, i).AsParameter(), values.ElementAt(i));
                    command.Parameters.Add(parameter);
                }
            }
        }

        /// <summary>
        /// Creates a parameter from object by mapping the property from the target entity type.
        /// </summary>
        /// <param name="command">The command object to be used.</param>
        /// <param name="param">The object to be used when creating the parameters.</param>
        public static void CreateParameters(this IDbCommand command, object param)
        {
            if (param == null)
            {
                return;
            }
            if (param is IEnumerable<PropertyValue>)
            {
                var propertyValues = (IEnumerable<PropertyValue>)param;
                foreach (var propertyValue in propertyValues)
                {
                    var dbType = propertyValue.Property.GetDbType();
                    command.Parameters.Add(command.CreateParameter(propertyValue.Name, propertyValue.Value, dbType));
                }
            }
            else if (param is ExpandoObject)
            {
                var dictionary = (IDictionary<string, object>)param;
                var dbType = (DbType?)null;
                foreach (var item in dictionary)
                {
                    var value = item.Value;
                    if (item.Value is CommandParameter)
                    {
                        var commandParameter = (CommandParameter)item.Value;
                        var property = commandParameter.MappedToType.GetTypeInfo().GetProperty(item.Key);
                        dbType = property?.GetCustomAttribute<TypeMapAttribute>()?.DbType ??
                            TypeMapper.Get(GetUnderlyingType(property?.PropertyType))?.DbType;
                        value = commandParameter.Value;
                    }
                    else
                    {
                        dbType = TypeMapper.Get(GetUnderlyingType(item.Value?.GetType()))?.DbType;
                    }
                    command.Parameters.Add(command.CreateParameter(item.Key, value, dbType));
                }
            }
            else
            {
                foreach (var property in param.GetType().GetTypeInfo().GetProperties())
                {
                    var dbType = property.GetCustomAttribute<TypeMapAttribute>()?.DbType ??
                        TypeMapper.Get(GetUnderlyingType(property.PropertyType))?.DbType;
                    command.Parameters.Add(command.CreateParameter(
                        PropertyMappedNameCache.Get(property), property.GetValue(param), dbType));
                }
            }
        }

        /// <summary>
        /// Gets the underlying type if present.
        /// </summary>
        /// <param name="type">The type where to get the underlying type.</param>
        /// <returns>The underlying type.</returns>
        private static Type GetUnderlyingType(Type type)
        {
            return type != null ? Nullable.GetUnderlyingType(type) ?? type : null;
        }
    }
}
