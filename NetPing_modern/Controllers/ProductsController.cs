using System.Linq;
using System.Web.Mvc;
using NetPing.DAL;
using NetPing_modern.Helpers;
using NetPing_modern.Models;
using WebGrease.Css.Extensions;
using NetPing_modern.ViewModels;
using NetPing.Models;
using System.Collections.Generic;

namespace NetPing_modern.Controllers
{
    public class ProductsController : Controller
    {
        private readonly IRepository _repository;

        public ProductsController(IRepository repository)
        {
            _repository = repository;
        }

        public ActionResult Compare(int[] compare)
        {
            var model = new DevicesCompare();

            if (compare == null || compare.Length < 2)
                return View(model);

            model.Devices = _repository.Devices.Where(d => compare.Contains(d.Id)).ToList();
            IEnumerable<DeviceParameter> collection = null;
            var deviceParameterEqualityComparer = new DeviceParameterEqualityComparer();
            for (int i = 0; i < model.Devices.Count - 1; i++)
            {
                var device = model.Devices[i];
                var next = model.Devices[i + 1];
                if (collection == null)
                {
                    collection = device.DeviceParameters.Union(next.DeviceParameters, deviceParameterEqualityComparer);
                }
                else
                {
                    collection = collection.Union(next.DeviceParameters, deviceParameterEqualityComparer);
                }
            }

            if (collection != null)
                model.Parameters = new List<DeviceParameter>(collection.Distinct(deviceParameterEqualityComparer));

            return View(model);
        }

        public ActionResult Device_view(string id)
        {

            var device = _repository.Devices.Where(dev => dev.Url == id).FirstOrDefault();

            if (device == null) return Redirect("/products");  // if key incorrect go to /products

            if (device.Name.Path.Contains("Development")) return Device_in_development(device);

            //Create list of connected devices
            var connected_devices = device.Connected_devices.Select(d => _repository.Devices.Where(dv => dv.Name == d).FirstOrDefault()).ToList();
            ViewBag.Connected_devices_accessuars = connected_devices.Where(d => d != null && !d.Name.Path.Contains("Sensors")).ToList();
            ViewBag.Connected_devices_sensors = connected_devices.Where(d => d != null && d.Name.Path.Contains("Sensors")).ToList();

            ViewBag.Parameter_groups = _repository.TermsDeviceParameters.Where(par => par.Level == 0).ToList();
            ViewBag.Files_groups = _repository.TermsFileTypes.Where(type => type.Level == 0).ToList();

            //Device group
            var dev_path = device.Name.Path.Split(';');
            var grp = dev_path[dev_path.Length - 2];
            var group_dev = _repository.Devices.FirstOrDefault(dev => dev.Name.OwnNameFromPath == grp);
            ViewBag.grp_name = group_dev.Name.Name;
            ViewBag.grp_url = Url.Action("Index", "Products", new { group = group_dev.Url });

            ViewBag.Title = device.Name.Name;
            ViewBag.Description = device.Name.Name;
            ViewBag.Keywords = device.Name.Name;
            return View("Device_view", device);
        }

        public ActionResult Solutions()
        {
            var solutionsNames = new[] { "dlja-servernyh-komnat-i-6kafov", "udaljonnoe-upravlenie-jelektropitaniem", "re6enija-na-osnove-POE" };
            var model = new ProductsModel
                      {
                          ActiveSection =
                             NavigationProvider.GetAllSections().First()
                      };
            var devices = new List<Device>();
            foreach (var solutionName in solutionsNames)
            {
                var sub = _repository.Devices.FirstOrDefault(d => d.Url == solutionName);
                if (sub != null)
                {
                    devices.AddRange(_repository.Devices.Where(d => !d.Name.IsGroup() && d.Name.IsUnderOther(sub.Name)));
                }
            }

            model.Devices = devices;
            model.ActiveSection.IsSelected = true;
            var sections = NavigationProvider.GetAllSections().Where(m => m.Url != model.ActiveSection.Url);
            sections.ForEach(m => model.Sections.Add(m));


            //return View(model);

            return View("Adaptive_Index", model);
        }

        public ActionResult Device_in_development(Device device)
        {
            ViewBag.Title = device.Name.Name;
            ViewBag.Description = device.Name.Name;
            ViewBag.Keywords = device.Name.Name;

            ViewBag.Parameter_groups = _repository.TermsDeviceParameters.Where(par => par.Level == 0).ToList();
            ViewBag.Files_groups = _repository.TermsFileTypes.Where(type => type.Level == 0).ToList();

            ViewBag.Step = device.Label.OwnNameFromPath;

            return View("Dev", device);
        }

        public ActionResult Development()
        {
            var devices = _repository.Devices.Where(d => !d.Name.IsGroup() && d.Name.Path.Contains("Development"));

            var model = new ProductsModel
            {
                ActiveSection =
                             NavigationProvider.GetAllSections().FirstOrDefault(m => m.Url == "development")
            };

            ViewBag.Title = ViewBag.Description = ViewBag.Keywords = model.ActiveSection.FormattedTitle;

            model.Devices = devices;

            return View("Adaptive_Index", model);
        }

        public ActionResult Index(string group, string id)
        {
            var devices = _repository.Devices.Where(d => !d.Name.IsGroup());
            var groups = _repository.Devices.Where(d => d.Name.IsGroup());
            if (group == null) return HttpNotFound();
            var g = _repository.Devices.FirstOrDefault(d => d.Url == @group);
            if (g != null)
            {
                if (!g.Name.IsGroup()) return Device_view(group);  // Open device page
                devices = devices.Where(d => !d.Name.IsGroup() && d.Name.IsUnderOther(g.Name));
            }
            else
            { return HttpNotFound(); }

            ViewBag.Title = g.Name.Name;
            ViewBag.Description = g.Name.Name;
            ViewBag.Keywords = g.Name.Name;

            var model = new ProductsModel
                        {
                            ActiveSection =
                                NavigationProvider.GetAllSections().FirstOrDefault(m => m.Url == @group)
                        };


            if (!string.IsNullOrEmpty(id))
            {
                var sub = _repository.Devices.FirstOrDefault(d => d.Url == id);
                if (sub != null)
                {
                    devices = _repository.Devices.Where(d => !d.Name.IsGroup() && d.Name.IsUnderOther(sub.Name));

                    ViewBag.Title = sub.Name.Name;
                    ViewBag.Description = sub.Name.Name;
                    ViewBag.Keywords = sub.Name.Name;
                }
                else { return HttpNotFound(); }
                model.ActiveSection = model.ActiveSection.Sections.First(m => m.Url == id);
            }
            else
            {
                model.ActiveSection.Sections.First().IsSelected = true;
            }
            model.Devices = devices;
            model.ActiveSection.IsSelected = true;
            var sections = NavigationProvider.GetAllSections().Where(m => m.Url != model.ActiveSection.Url);
            sections.ForEach(m => model.Sections.Add(m));


            //return View(model);

            return View("Adaptive_Index", model);
        }
    }
}