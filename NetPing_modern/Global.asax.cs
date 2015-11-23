using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using NetPing.DAL;
using NetPing.Global.Config;
using NetPing_modern.Services.Confluence;

namespace NetPing
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();

            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            var cfg = new Config();

            var factory = new SharepointClientFactory(cfg);

            var inFileDataStorage = new InFileDataStorage();

            var sw = Stopwatch.StartNew();

            var sync = new DataStorageUpdater(inFileDataStorage, factory, new ConfluenceClient(new Config()));


            sync.Update();

            sw.Stop();

            var elapsed = sw.ElapsedMilliseconds;

            Debug.WriteLine("");
        }
    }
}