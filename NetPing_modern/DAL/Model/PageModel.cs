using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPing_modern.DAL.Model
{
    [Serializable]
    public class PageModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public ICollection<PageModel> Pages { get; set; }
    }
}
