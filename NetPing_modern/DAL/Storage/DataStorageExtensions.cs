using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NetPing.Models;

namespace NetPing.DAL
{
    internal static class DataStorageExtensions
    {
        public static IReadOnlyCollection<SPTerm> GetNames(this IDataStorage storage)
        {
            return GetTermsColection(storage, CacheKeys.Names);
        }

        public static IReadOnlyCollection<SPTerm> GetDeviceParameterNames(this IDataStorage storage)
        {
            return GetTermsColection(storage, CacheKeys.DeviceParameterNames);
        }

        public static IReadOnlyCollection<SPTerm> GetDocumentTypes(this IDataStorage storage)
        {
            return GetTermsColection(storage, CacheKeys.DocumentTypes);
        }

        public static IReadOnlyCollection<SPTerm> GetPostCategories(this IDataStorage storage)
        {
            return GetTermsColection(storage, CacheKeys.PostCategories);
        }

        private static IReadOnlyCollection<SPTerm> GetTermsColection(IDataStorage storage, String name)
        {
            var names = storage.Get<SPTerm>(name).ToList();

            return new ReadOnlyCollection<SPTerm>(names);
        }
    }
}