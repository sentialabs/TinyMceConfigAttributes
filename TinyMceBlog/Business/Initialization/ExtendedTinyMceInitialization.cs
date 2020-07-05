using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using TinyMceBlog.Models.Blocks;
using TinyMceBlog.Models.Pages;
using EPiServer.Cms.TinyMce.Core;
using EPiServer.Core;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TinyMceBlog.Business.Attributes;
using TinyMceBlog.Business.TinyMceConfig;

namespace TinyMceBlog.Business.Initialization
{
    [ModuleDependency(typeof(DependencyResolverInitialization), typeof(TinyMceInitialization))]
    public class ExtendedTinyMceInitialization : IConfigurableModule
    {
        private List<ITinyMceConfiguration> _tinyMceConfigurations;

        public void Initialize(InitializationEngine context)
        {
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            //var properties = type
            //    .GetProperties().Where(prop => prop.GetCustomAttributes(typeof(SimpleTinyMce), true).Any());
            //        //.Any(x => x.AttributeType == SimpleTinyMce.getT).ToList();


            ////This works for a concrete type
            //var properties = type
            //    .GetProperties().Where(x => x.CustomAttributes.Any(att => att.AttributeType == typeof(SimpleTinyMceAttribute))).ToList();

            //var properties = type.GetProperties().Where(x => x.GetCustomAttributesData())

            var tinyMceConfigurations = ServiceLocator.Current.GetAllInstances<ITinyMceConfiguration>();
            _tinyMceConfigurations = tinyMceConfigurations.ToList();

            context.Services.Configure<TinyMceConfiguration>(config =>
            {

                foreach (var tinyMceConfiguration in _tinyMceConfigurations)
                {
                    Console.WriteLine(tinyMceConfiguration.GetType());
                }

                // Add content CSS to the default settings.
                config.Default()
                    .ContentCss("/static/css/editor.css");

                // This will clone the default settings object and extend it by
                // limiting the block formats for the MainBody property of an ArticlePage.
                config.For<ArticlePage>(t => t.MainBody)
                    .BlockFormats("Paragraph=p;Header 1=h1;Header 2=h2;Header 3=h3");

                // Passing a second argument to For<> will clone the given settings object
                // instead of the default one and extend it with some basic toolbar commands.
                config.For<EditorialBlock>(t => t.MainBody, config.Empty())
                    .AddEpiserverSupport()
                    .DisableMenubar()
                    .Toolbar("bold italic underline strikethrough");


                var simpleConfig = config.Default().Clone()
                    .AddPlugin("wordcount code")
                    .AppendToolbar("code")
                    .RemovePlugin("image")
                    .RemovePlugin("epi-image-editor")
                    .Height(125);

                var listOfTypes = (
                    from domainAssembly in AppDomain.CurrentDomain.GetAssemblies()
                    from assemblyType in domainAssembly.GetTypes()
                    where typeof(ContentData).IsAssignableFrom(assemblyType)
                    select assemblyType).ToArray();

                foreach (var contentType in listOfTypes)
                {
                    var properties = contentType
                        .GetProperties().Where(x => x.CustomAttributes.Any(att => att.AttributeType == typeof(SimpleTinyMceAttribute))).ToList();

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

                            var parameters = new object[] { lambda, simpleConfig };

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

                    //config.For<StandardPage>(x => x.MainBody, simpleConfig);
                }


            });
        }
    }
}
