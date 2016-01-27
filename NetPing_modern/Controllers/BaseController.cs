using NetPing.DAL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace  NetPing_modern.Controllers
{
    public class BaseController : Controller
    {
        protected override IAsyncResult BeginExecute(System.Web.Routing.RequestContext requestContext, AsyncCallback callback, object state)
        {
            var injections = NetpingHelpers.Helpers.GetHtmlInjections();
            var allPages = injections.Where(x => x.Page == "All");

            string head = string.Empty;
            string body = string.Empty;
            foreach(var injection in injections)
            {
                if (injection.Section == "Head")
                    head += injection.HTML;
                else
                    body += injection.HTML;
            }

            ViewBag.HeadInjections = head;
            ViewBag.BodyInjections = body;

            return base.BeginExecute(requestContext, callback, state);
        }
    }
}