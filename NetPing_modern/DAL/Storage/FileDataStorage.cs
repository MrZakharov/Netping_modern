using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using NLog;

namespace NetPing.DAL
{
    internal class FileDataStorage : IDataStorage
    {
        public const String UserGuidFolder = "UserGuides";

        private const String StoreRootPath = "Content\\Data";
        
        private readonly ConcurrentDictionary<String, Object> _locks = new ConcurrentDictionary<String, Object>();

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Storage);

        public IEnumerable<T> Get<T>(StorageKey key)
        {
            try
            {
                var filePath = CreateFilePath(key);

                var lockRoot = GetLock(key);

                lock (lockRoot)
                {
                    if (File.Exists(filePath))
                    {
                        using (var fileStream = File.OpenRead(filePath))
                        {
                            var formatter = new BinaryFormatter();

                            var storedObject = formatter.Deserialize(fileStream);

                            return (IEnumerable<T>) storedObject;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unable to find storage file by key '{key}'");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to get collection by key: '{key}'. Item type: {typeof(T)}");

                throw;
            }
        }

        public void Set<T>(StorageKey key, IEnumerable<T> collection)
        {
            try
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection), "Unable to set null collection");
                }

                var filePath = CreateFilePath(key);

                var lockRoot = GetLock(key);

                lock (lockRoot)
                {
                    using (var fileStream = File.Create(filePath))
                    {
                        var formatter = new BinaryFormatter();

                        formatter.Serialize(fileStream, collection);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to set collection by key: '{key}'. Item type: {typeof (T)}");

                throw;
            }
        }
        
        public void Append<T>(StorageKey key, IEnumerable<T> collection)
        {
            try
            {
                if (collection == null)
                {
                    throw new ArgumentNullException(nameof(collection), "Unable to append null collection");
                }

                var filePath = CreateFilePath(key);

                if (!File.Exists(filePath))
                {
                    Set(key, collection);
                }
                else
                {
                    IEnumerable<T> currentCollection = Get<T>(key);

                    var combinedCollection =  currentCollection.Union(collection).ToList();
                    Set(key, combinedCollection);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to append collection by key: '{key}'. Item type: {typeof(T)}");

                throw;
            }
        }

        private Object GetLock(StorageKey key)
        {
            return _locks.GetOrAdd(key.Name, new Object());
        }

        private String CreateFilePath(StorageKey key)
        {
            try
            {
                var languageTag = CultureInfo.CurrentCulture.IetfLanguageTag;

                var fileName = $"{key.Name}_{languageTag}.dat";

                var filePath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, 
                    StoreRootPath, 
                    key.Directory,
                    fileName);

                return filePath;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to create path to storage with key: '{key}'");

                throw;
            }
        }
    }
}