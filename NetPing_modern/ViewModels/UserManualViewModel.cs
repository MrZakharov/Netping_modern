using NetPing_modern.DAL.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPing_modern.ViewModels
{
    public class UserManualViewModel
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public IOrderedEnumerable<PageModel> Pages { get; set; }

        public IEnumerable<UserManualSectionViewModel> Sections { get; set; }
    }

    public class UserManualSectionViewModel
    {
        public string Name { get; set; }

        public IEnumerable<UserManualDeviceViewModel> Devices { get; set; }
    }

    public class UserManualDeviceViewModel
    {
        public string Name { get; set; }

        public IEnumerable<UserManualFiles> UserGuides { get; set; }
    }

    public class UserManualFiles
    {
        public string Name { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public int Id { get; set; }

        public IEnumerable<UserManualPageViewModel> Pages { get; set; }
    }

    public class UserManualPageViewModel
    {
        public string Name { get; set; }
        public string Url { get; set; }

        public IEnumerable<UserManualSubPagesVideModel> SubPages { get; set; }
    }

    public class UserManualSubPagesVideModel
    {
        public string Name { get; set; }
        public string Url { get; set; }
    }
}
