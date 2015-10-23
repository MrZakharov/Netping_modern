using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NetPing.DAL;
using NetPing.Models;
using NetPing_modern.Properties;
using System.Resources;
using NetPing_modern.Controllers;

namespace NetPing.Controllers
{
    public class InnerPagesController : BaseController
    {
        //
        // GET: /InnerPages/

        private IRepository _repository;

        public InnerPagesController(IRepository repository)
        {
            _repository = repository;
        }
        /*
                public ActionResult Question()
                {
                    return View("Question");
                }
        */
        public ActionResult UCache()
        {
            ViewBag.result = _repository.UpdateAll();

            return View();
        }

        #region Async Cache update

        public ActionResult UCacheAsync()
        {
            return View();
        }

        [HttpPost]
        public JsonResult UCacheAsyncWork(string dataName)
        {
            try
            {
                return Json(_repository.UpdateAllAsync(dataName));
            }
            catch (Exception)
            {
                try
                {
                    return Json(_repository.UpdateAllAsync(dataName));
                }
                catch (Exception Ex)
                {
                    return Json(Ex.ToString());
                }
            }
        }

        #endregion

        public ActionResult Buy(string id)
        {
            ResourceManager resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.Buy", typeof(InnerPagesController).Assembly); ;
            switch (id)
            {
                case "":
                case null:
                    ViewBag.Text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Buy").Text;
                    break;
                case "dealers":
                    resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.Dealers", typeof(InnerPagesController).Assembly);
                    ViewBag.Text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Dealers").Text;
                    break;
                case "partnership-how-to":
                    resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.Partnership", typeof(InnerPagesController).Assembly);
                    ViewBag.Text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Partnership-how").Text;
                    break;

                default:
                    return HttpNotFound();
            }
            ViewBag.Head = resourceManager.GetString("Page_head", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Title = resourceManager.GetString("Page_title", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Description = resourceManager.GetString("Page_description", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Keywords = resourceManager.GetString("Page_keywords", System.Globalization.CultureInfo.CurrentCulture);
            return View("InnerPage");
        }

        public ActionResult About()
        {
            var text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "About").Text;
            //            if (text == null) return HttpNotFound();
            var resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.About", typeof(InnerPagesController).Assembly);

            ViewBag.Text = text;
            ViewBag.Head = resourceManager.GetString("Page_head", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Title = resourceManager.GetString("Page_title", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Description = resourceManager.GetString("Page_description", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Keywords = resourceManager.GetString("Page_keywords", System.Globalization.CultureInfo.CurrentCulture);

            return View("InnerPage");
        }

        public ActionResult Contacts()
        {
            var text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Contact_us").Text;
            //            if (text == null) return HttpNotFound();

            var resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.Contacts", typeof(InnerPagesController).Assembly);

            ViewBag.Text = text;
            ViewBag.Head = resourceManager.GetString("Page_head", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Title = resourceManager.GetString("Page_title", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Description = resourceManager.GetString("Page_description", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Keywords = resourceManager.GetString("Page_keywords", System.Globalization.CultureInfo.CurrentCulture);

            ViewBag.Text = text;
            return View("InnerPage");
        }

        public ActionResult Vacancy()
        {
            var text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Vacancy").Text;
            //            if (text == null) return HttpNotFound();

            ViewBag.Head = "Мы ищем сотрудников!";
            ViewBag.Text = text;
            ViewBag.Title = "Вакансии компании Netping";
            ViewBag.Description = "список вакансий компании Netping";
            ViewBag.Keywords = "вакансии";

            return View("InnerPage");
        }

        public ActionResult Support()
        {
            var text = _repository.SiteTexts.FirstOrDefault(t => t.Tag == "Support").Text;
            //            if (text == null) return HttpNotFound();
            var resourceManager = new ResourceManager("NetPing_modern.Resources.Views.InnerPages.Support", typeof(InnerPagesController).Assembly);

            ViewBag.Text = text;
            ViewBag.Head = resourceManager.GetString("Page_head", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Title = resourceManager.GetString("Page_title", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Description = resourceManager.GetString("Page_description", System.Globalization.CultureInfo.CurrentCulture);
            ViewBag.Keywords = resourceManager.GetString("Page_keywords", System.Globalization.CultureInfo.CurrentCulture);

            return View("InnerPage");
        }

        public ActionResult Dev(string id)
        {
            return View();
        }


    }
}
