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
        public String Address { get; set; }
        public String Requisites { get; set; }
        public String Phone { get; set; }
        public String Shipping { get; set; }
        public IEnumerable<IDictionary<string, string>> Data { get; set; }
    }
}