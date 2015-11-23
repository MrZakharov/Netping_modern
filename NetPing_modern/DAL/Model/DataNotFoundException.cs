using System;

namespace NetPing.DAL
{
    internal class DataNotFoundException : Exception
    {
        public DataNotFoundException()
        {
        }

        public DataNotFoundException(String message, Exception innerException) : base(message, innerException)
        {
        }

        public DataNotFoundException(String message) : base(message)
        {
        }
    }
}