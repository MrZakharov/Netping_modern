using System;
using System.Collections.Generic;

namespace NetPing.DAL
{
    internal interface IDataStorage
    {
        IEnumerable<T> Get<T>(String key);

        void Set<T>(String key, IEnumerable<T> collection);

        void Append<T>(String key, IEnumerable<T> collection);
    }
}