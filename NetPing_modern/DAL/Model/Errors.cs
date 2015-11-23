using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace NetPing.DAL
{
    public class Errors
    {
        public string Message { get; set; }

        public Errors(string message)
        {
            Message = message;
        }
    }
}