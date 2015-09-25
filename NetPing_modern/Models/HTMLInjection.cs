using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetPing.Models
{
    [Serializable]
    public class HTMLInjection
    {
        public string Title { get; set; }
        public string Section { get; set; }
        public string Page { get; set; }
        public string HTML { get; set; }
    }
}
