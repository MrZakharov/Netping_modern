using System;
using System.Collections.Generic;
using System.Web;
using NetPing.Tools;

namespace NetPing.DAL
{
    internal class CachingProxy
    {
        private readonly IDataStorage _storage;

        public CachingProxy(IDataStorage storage)
        {
            _storage = storage;
        }

        public IEnumerable<T> GetAndCache<T>(StorageKey key)
        {
            var cachedCollection = HttpRuntime.Cache.Get(key.Name);

            if (cachedCollection != null)
            {
                // Возвращаем найденную в кэше коллекцию
                return (IEnumerable<T>) cachedCollection;
            }

            try
            {
                var storedCollection = _storage.Get<T>(key);

                // Кэшируем найденную в хранилище коллекцию
                HttpRuntime.Cache.Insert(key.Name, storedCollection, new TimerCacheDependency());

                // Возвращаем найденную в хранилище коллекцию
                return storedCollection;
            }
            catch (Exception ex)
            {
                throw new DataNotFoundException("Unable to find data collection in storage", ex);
            }
        }
    }
}