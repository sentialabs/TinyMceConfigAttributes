using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Cms.TinyMce.Core;
using EPiServer.Core;

namespace TinyMceBlog.Business.TinyMceConfig
{
    public static class TinyMceCustomConfigRegistration
    {
        /// <summary>
        /// Sets custom TinyMceSettings on all XhtmlString properties
        /// decorated with a specific attribute.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="attributeType">The type of the attribute used to decorate the property</param>
        /// <param name="customTinyMceSettings">The custom TinyMceSettings</param>
        public static void RegisterCustomTinyMceSettingsAttribute(TinyMceConfiguration config, 
            Type attributeType, 
            TinyMceSettings customTinyMceSettings)
        {
            var listOfTypes = GetListOfEpiserverContentTypes();

            foreach (var contentType in listOfTypes)
            {
                var properties = contentType
                    .GetProperties().Where(x => x.CustomAttributes.Any(att => att.AttributeType == attributeType)).ToList();

                if (!properties.Any()) continue;

                Console.WriteLine(contentType.Name + " " + properties.FirstOrDefault()?.Name);

                var theMethod = typeof(TinyMceConfiguration).GetMethod("For");

                if (theMethod == null) continue;

                var theGenericMethod = theMethod.MakeGenericMethod(contentType);

                foreach (var propertyInfo in properties)
                {
                    var entityType = propertyInfo.ReflectedType;
                    if (entityType != null)
                    {
                        var parameter = Expression.Parameter(entityType, "entity");
                        var property = Expression.Property(parameter, propertyInfo);
                        var funcType = typeof(Func<,>).MakeGenericType(entityType, typeof(object));
                        var lambda = Expression.Lambda(funcType, property, parameter);

                        var parameters = new object[] { lambda, customTinyMceSettings };

                        try
                        {
                            theGenericMethod.Invoke(config, parameters);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get a list of all types which may
        /// have an XhtmlString property (Pages and Blocks).
        /// </summary>
        /// <returns>A list of types</returns>
        private static IEnumerable<Type> GetListOfEpiserverContentTypes()
        {
            var listOfTypes = (
                from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                from assemblyType in domainAssembly.GetTypes()
                where typeof(ContentData).IsAssignableFrom(assemblyType)
                select assemblyType).ToArray();

            return listOfTypes;
        }
    }
}