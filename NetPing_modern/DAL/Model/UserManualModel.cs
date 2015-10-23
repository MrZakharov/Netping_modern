using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPing_modern.DAL.Model
{
    [Serializable]
    public class UserManualModel
    {
        public int Id { get; set; }
        public int ItemId { get; set; }
        public string Title { get; set; }
        public string Name { get; set; }
        public ICollection<PageModel> Pages { get; set; }
    }
}
