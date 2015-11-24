using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;

namespace NetPing.DAL
{
    internal class InFileDataStorage : IDataStorage
    {
        private const String StoreRoot = "Content\\Data";
        
        private readonly ConcurrentDictionary<String, Object> _locks = new ConcurrentDictionary<String, Object>();

        public IEnumerable<T> Get<T>(String key)
        {
            var filePath = CreateFilePath(key);

            var lockRoot = _locks.GetOrAdd(key, new Object());

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
                    throw new InvalidOperationException($"Unable to find cache file by name '{key}'");
                }
            }
        }

        public void Set<T>(String key, IEnumerable<T> collection)
        {
            var filePath = CreateFilePath(key);

            var lockRoot = _locks.GetOrAdd(key, new Object());

            lock (lockRoot)
            {
                using (var fileStream = File.Create(filePath))
                {
                    var formatter = new BinaryFormatter();

                    formatter.Serialize(fileStream, collection);
                }
            }
        }

        public void Append<T>(String key, IEnumerable<T> collection)
        {
            var filePath = CreateFilePath(key);

            if (File.Exists(filePath))
            {
                Set(key, collection);
            }
            else
            {
                var currentCollection = Get<T>(key);

                var combinedCollection = currentCollection.Union(collection);

                Set(key, combinedCollection);
            }
        }

        private String CreateFilePath(string key)
        {
            var languageTag = CultureInfo.CurrentCulture.IetfLanguageTag;

            var fileName = $"{key}_{languageTag}.dat";

            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, StoreRoot, fileName);

            return filePath;
        }
    }
}