using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using NetPing.Models;
using NetPing_modern.Resources;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class InFileDataStorageSynchronizer
    {
        private readonly IDataStorage _storage;
        private readonly SharepointClientParameters _sharepointClientParameters;
        private readonly ConfluenceClient _confluenceClient;

        public InFileDataStorageSynchronizer(IDataStorage storage, SharepointClientParameters sharepointClientParameters, ConfluenceClient confluenceClient)
        {
            _storage = storage;
            _sharepointClientParameters = sharepointClientParameters;
            _confluenceClient = confluenceClient;
        }

        public void Load()
        {
            #region :: Step 1 ::

            var sw = Stopwatch.StartNew();

            Parallel.Invoke(
                () => LoadSharepointTerms(CacheKeys.Names),
                () => LoadSharepointTerms(CacheKeys.Purposes),
                () => LoadSharepointTerms(CacheKeys.DeviceParameterNames),
                () => LoadSharepointTerms(CacheKeys.Labels),
                () => LoadSharepointTerms(CacheKeys.PostCategories),
                () => LoadSharepointTerms(CacheKeys.DocumentTypes),
                () => LoadSharepointList(CacheKeys.HtmlInjection, Camls.HtmlInjection, new HtmlInjectionConverter()),
                () => LoadSharepointList(CacheKeys.SiteTexts, Camls.SiteTexts, new SiteTextConverter(_confluenceClient)));

            sw.Stop();

            var el = sw.ElapsedMilliseconds;

            Debug.WriteLine(el);

            #endregion

            #region :: Step 2 ::

            var devicePhotoConverter = new DevicePhotoConverter(_storage.Get<SPTerm>(CacheKeys.Names));
            var deviceParameterConverter = new DeviceParameterConverter(_storage.Get<SPTerm>(CacheKeys.DeviceParameterNames), _storage.Get<SPTerm>(CacheKeys.Names));
            var pubFilesConverter = new PubFilesConverter(_storage.Get<SPTerm>(CacheKeys.DocumentTypes));
            var postConverter = new PostConverter(_confluenceClient, _storage.Get<SPTerm>(CacheKeys.Names), _storage.Get<SPTerm>(CacheKeys.PostCategories));

            Parallel.Invoke(
                () => LoadSharepointList(CacheKeys.DevicePhotos, Camls.DevicePhotos, devicePhotoConverter),
                () => LoadSharepointList(CacheKeys.DeviceParameters, String.Empty, deviceParameterConverter),
                () => LoadSharepointList(CacheKeys.PubFiles, Camls.PubFiles, pubFilesConverter),
                () => LoadSharepointList(CacheKeys.Posts, Camls.Posts, postConverter));

            #endregion
        }

        private void LoadSharepointList<T>(String listName, String query, IListItemConverter<T> converter)
        {
            var convertedList = GetSharepointList(listName, query, converter);

            _storage.Insert(listName, convertedList);
        }

        private IEnumerable<T> GetSharepointList<T>(String listName, String query, IListItemConverter<T> converter)
        {
            using (var sp = new SharepointClient(_sharepointClientParameters))
            {
                var sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(listName);

                var items = sp.GetList(sharepointName, query);

                var convertedList = items.ToList().Select(converter.Convert).ToList();

                return convertedList;
            }
        }

        private void LoadSharepointTerms(String setName)
        {
            using (var sp = new SharepointClient(_sharepointClientParameters))
            {
                var sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(setName);

                var nameTerms = sp.GetTerms(sharepointName);

                _storage.Insert(setName, nameTerms);
            }
        }
    }
}