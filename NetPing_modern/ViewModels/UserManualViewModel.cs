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
        public string Title { get; set; }
        public IOrderedEnumerable<PageModel> Pages { get; set; }
    }
}
