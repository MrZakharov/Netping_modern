using System.Linq;
using AutoMapper;
using NetPing.DAL;
using NetPing.Global.Config;
using NetPing.Models;
using NetPing.PriceGeneration.YandexMarker;
using NetPing_modern.Mappers;
using NetPing_modern.Services.Confluence;
using NetPing_modern.ViewModels;
using Ninject.Extensions.Conventions;


[assembly: WebActivatorEx.PreApplicationStartMethod(typeof(NetPing_modern.App_Start.NinjectWebCommon), "Start")]
[assembly: WebActivatorEx.ApplicationShutdownMethodAttribute(typeof(NetPing_modern.App_Start.NinjectWebCommon), "Stop")]

namespace NetPing_modern.App_Start
{
    using System;
    using System.Web;

    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;

    public static class NinjectWebCommon 
    {
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            var kernel = new StandardKernel();
            kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
            kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();
            
            RegisterServices(kernel);
            ConfigureMapping(kernel);
            return kernel;
        }

        private static void ConfigureMapping(IKernel kernel)
        {
            Mapper.Initialize(cfg => 
                              {
                                  cfg.ConstructServicesUsing(t => kernel.Get(t));
                                  foreach (var profile in typeof (NinjectWebCommon).Assembly.GetTypes()
                                      .Where(t => t.GetInterfaces().Any(x => x.IsGenericType && 
                                          x.GetGenericTypeDefinition() == typeof(IMapper<,>)) && !t.IsGenericType))
                                  {
                                      cfg.AddProfile(kernel.Get(profile) as Profile);
                                  }
                              });

            Mapper.AssertConfigurationIsValid();
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
            kernel.Bind<ISharepointClientFactory>().To<SharepointClientFactory>().InSingletonScope();
            kernel.Bind<IRepository>().To<SPOnlineRepository>().InRequestScope();
            kernel.Bind<IConfig>().To<Config>().InSingletonScope();
            kernel.Bind<IConfluenceClient>().To<ConfluenceClient>().InRequestScope();
            kernel.Bind(typeof (IMapper<,>)).To(typeof (DefaultMapper<,>));
            kernel.Bind<IMapper<Post, PostViewModel>>().To<PostViewModelMapper>();
            kernel.Bind<IMapper<SPTerm, TermViewModel>>().To<TermViewModelMapper>();
            kernel.Bind<IMapper<SPTerm, CategoryViewModel>>().To<CategoryViewModelMapper>();

        }
    }
}
