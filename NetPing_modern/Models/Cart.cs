using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NetPing_modern.ViewModels
{
    public class Cart
    {
        public String Name { get; set; }
        public String EMail { get; set; }
        public String Comment { get; set; }
        public IEnumerable<IDictionary<string, string>> Data { get; set; }
    }
}