using System;

namespace NetPing.DAL
{
    internal class SharepointClientParameters
    {
        public String Url { get; set; }

        public Int32 RequestTimeout { get; set; }

        public String User { get; set; }

        public String Password { get; set; }
    }
}