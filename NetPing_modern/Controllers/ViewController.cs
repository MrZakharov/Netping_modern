using System.Web.Mvc;
using NetPing.DAL;
using System.Collections.Generic;
using System.Linq;
using System;
using NetPing_modern.Controllers;


namespace NetPing.Controllers
{
    public class ViewController : BaseController
    {
        private readonly IRepository _repository;

        public ViewController(IRepository repository)
        {
            _repository = repository;
        }

        //
        // GET: /View/



        public ActionResult Index(string id)
        {
            Int16 id_int = 0;
            if (!Int16.TryParse(id,out id_int))  return HttpNotFound();

            var post=_repository.Posts.FirstOrDefault(p => p.Id == id_int);
            if (post == null) return HttpNotFound();
            return RedirectPermanent(post.Url_name);
           
        }

      
    }
}
