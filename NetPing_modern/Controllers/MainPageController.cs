using System.Linq;
using System.Web.Mvc;
using NetPing.DAL;
using NetPing_modern.Controllers;

namespace NetPing.Controllers
{
    public class MainPageController : BaseController
    {
        private readonly IRepository _repository;

        public MainPageController(IRepository repository)
        {
            _repository = repository;
        }

        //
        // GET: /MainPage/

        public ActionResult Index()
        {
            ViewBag.posts = _repository.Posts.OrderByDescending(item => item.Created);
            
            ViewBag.Devices = NetpingHelpers.Helpers.GetNewDevices();

            ViewBag.DevDevices = NetpingHelpers.Helpers.GetDevDevices();
            //Main page banners list
            
            // var banners = repository.pubfiles;
           
            //string banner_string = "";
            //foreach (var banner in banners )
            //{
            //    banner_string = banner_string + '"'+banner.url+'"'+",";
            //}
            //viewbag.banners = banner_string.trimend(',');

            ViewBag.Banners = _repository.PubFiles;

            return View();
        }

    }
}
