using System;
using System.Web.Http;
using System.Web.Mvc;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;
using EPiServer.ServiceLocation;
using TinyMceBlog.Business.Rendering;
using EPiServer.Web.Mvc;
using EPiServer.Web.Mvc.Html;
using Indivirtual.LUMC.Web.Business.Initialization;
using StructureMap;
using TinyMceBlog.Business.TinyMceConfig;

namespace TinyMceBlog.Business.Initialization
{
    [InitializableModule]
    public class DependencyResolverInitialization : IConfigurableModule
    {
        public void ConfigureContainer(ServiceConfigurationContext context)
        {
            var container = context.StructureMap();
            if (container == null)
            {
                string message = "ServiceConfigurationContext.StructureMap() returned null. StructureMapDependencyResolver cannot be initialized.";
                throw new InvalidOperationException(message);
            }

            var resolver = new StructureMapDependencyResolver(container);
            DependencyResolver.SetResolver(resolver); // For MVC
            GlobalConfiguration.Configuration.DependencyResolver = resolver; // For WebAPI     

            container.Configure(ConfigureContainer);

            //Implementations for custom interfaces can be registered here.

            context.ConfigurationComplete += (o, e) =>
            {
                //Register custom implementations that should be used in favour of the default implementations
                context.Services.AddTransient<IContentRenderer, ErrorHandlingContentRenderer>()
                    .AddTransient<ContentAreaRenderer, AlloyContentAreaRenderer>();
            };
        }

        private static void ConfigureContainer(ConfigurationExpression container)
        {
            container.For<ITinyMceConfiguration>().Use<SimpleTinyMce>();
            container.For<ITinyMceConfiguration>().Use<IntermediateTinyMce>();
            container.For<ITinyMceConfiguration>().Use<ElaborateTinyMce>();
        }

        public void Initialize(InitializationEngine context)
        {
            DependencyResolver.SetResolver(new ServiceLocatorDependencyResolver(context.Locate.Advanced));
        }

        public void Uninitialize(InitializationEngine context)
        {
        }

        public void Preload(string[] parameters)
        {
        }
    }
}
