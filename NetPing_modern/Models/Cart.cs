using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NetPing_modern.ViewModels
{
    public class Cart
    {
        [Required]
        public String Name { get; set; }
        [Required]
        public String EMail { get; set; }
        [Required]
        public String Address { get; set; }
        [Required]
        public String IndexCode { get; set; }

        public String Requisites { get; set; }
        [Required]
        public String Phone { get; set; }
        public String Shipping { get; set; }
        public IEnumerable<IDictionary<string, string>> Data { get; set; }
    }
}