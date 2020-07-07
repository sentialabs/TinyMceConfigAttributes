using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using EPiServer.Cms.TinyMce.Core;
using EPiServer.Core;
using TinyMceBlog.Business.Attributes;

namespace TinyMceBlog.Business.TinyMceConfig
{
    public static class TinyMceCustomSettingsAttributeRegistration<T> where T : BaseTinyMceCustomSettingsAttribute
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly IEnumerable<Type> _listOfEpiserverContentDataTypes;

        static TinyMceCustomSettingsAttributeRegistration()
        {
            _listOfEpiserverContentDataTypes = GetListOfEpiserverContentTypes();
        }

        /// <summary>
        /// Sets custom TinyMceSettings on all XhtmlString properties
        /// decorated with a specific attribute.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="customTinyMceSettings">The custom TinyMceSettings</param>
        public static void RegisterCustomTinyMceSettingsAttribute(TinyMceConfiguration config,
            TinyMceSettings customTinyMceSettings)
        {
            // Get MethodInfo for the extension method usually used to designate
            // custom TinyMceSettings for an XhtmlProperty, viz.
            // config.For<StandardPage>(x => x.MainBody, customTinyMceSettings);
            var theForMethod = typeof(TinyMceConfiguration).GetMethod("For");

            if (theForMethod == null) return;

            foreach (var contentType in _listOfEpiserverContentDataTypes)
            {
                // Get the properties decorated with the attribute used for designating the custom TinyMceSettings.
                var properties = contentType
                    .GetProperties().Where(x => x.CustomAttributes.Any(att => att.AttributeType == typeof(T))).ToList();

                if (!properties.Any()) continue;

                // Make the For method generic.
                var theGenericMethod = theForMethod.MakeGenericMethod(contentType);

                foreach (var propertyInfo in properties)
                {
                    // Continue if the attribute is inadvertently applied to a
                    // property which is not an XhtmlString.
                    if (propertyInfo.PropertyType.Name != "XhtmlString") continue;
                    
                    var parameter = Expression.Parameter(contentType, "entity");
                    var property = Expression.Property(parameter, propertyInfo);
                    var funcType = typeof(Func<,>).MakeGenericType(contentType, typeof(object));
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