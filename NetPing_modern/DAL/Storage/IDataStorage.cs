using System.Collections.Generic;

namespace NetPing.DAL
{
    internal interface IDataStorage
    {
        IEnumerable<T> Get<T>(StorageKey key);

        void Set<T>(StorageKey key, IEnumerable<T> collection);

        void Append<T>(StorageKey key, IEnumerable<T> collection);
    }
}