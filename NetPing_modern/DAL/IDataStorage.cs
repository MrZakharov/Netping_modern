using System;
using System.Collections.Generic;

namespace NetPing.DAL
{
    internal interface IDataStorage
    {
        IEnumerable<T> Get<T>(String key);

        void Insert<T>(String key, IEnumerable<T> collection);
    }
}