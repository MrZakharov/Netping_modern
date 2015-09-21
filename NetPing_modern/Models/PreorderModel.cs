using NetPing.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace NetPing_modern.Models
{
    public class PreorderModel
    {
        [Required]
        public string Name { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Comment { get; set; }

        public Device Device { get; set; }
    }
}