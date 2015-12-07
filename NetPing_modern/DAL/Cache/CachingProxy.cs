using System;
using System.Collections.Generic;
using System.Web;
using NetPing.Tools;
using NLog;

namespace NetPing.DAL
{
    internal class CachingProxy
    {
        private readonly IDataStorage _storage;

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Cache);

        public CachingProxy(IDataStorage storage)
        {
            _storage = storage;
        }

        public IEnumerable<T> GetAndCache<T>(StorageKey key)
        {
            var cachedCollection = HttpRuntime.Cache.Get(key.Name);

            if (cachedCollection != null)
            {
                return (IEnumerable<T>) cachedCollection;
            }

            try
            {
                var storedCollection = _storage.Get<T>(key);

                HttpRuntime.Cache.Insert(key.Name, storedCollection, new TimerCacheDependency());

                return storedCollection;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Get collection from storage error");

                throw new DataNotFoundException("Unable to find data collection in storage", ex);
            }
        }
    }
}