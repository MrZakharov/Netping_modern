using System;
using System.Collections.Generic;

namespace NetPing.DAL
{
    internal static class SharepointToCacheNamesMapper
    {
        private static readonly Dictionary<string, string> _dictionary = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> _revDictionary = new Dictionary<string, string>();

        static SharepointToCacheNamesMapper()
        {
            _dictionary.Add(CacheKeys.Names, SharepointKeys.Names);
            _dictionary.Add(CacheKeys.Purposes, SharepointKeys.Purposes);
            _dictionary.Add(CacheKeys.DeviceParameterNames, SharepointKeys.DeviceParameterNames);
            _dictionary.Add(CacheKeys.Labels, SharepointKeys.Labels);
            _dictionary.Add(CacheKeys.PostCategories, SharepointKeys.PostCategories);
            _dictionary.Add(CacheKeys.DocumentTypes, SharepointKeys.DocumentTypes);
            _dictionary.Add(CacheKeys.HtmlInjection, SharepointKeys.HtmlInjection);
            _dictionary.Add(CacheKeys.SiteTexts, SharepointKeys.SiteTexts);
            _dictionary.Add(CacheKeys.DevicePhotos, SharepointKeys.DevicePhotos);
            _dictionary.Add(CacheKeys.DeviceParameters, SharepointKeys.DeviceParameters);
            _dictionary.Add(CacheKeys.PubFiles, SharepointKeys.PubFiles);
            _dictionary.Add(CacheKeys.Posts, SharepointKeys.Posts);

            foreach (var item in _dictionary)
            {
                _revDictionary.Add(item.Value, item.Key);
            }
        }

        public static String CacheKeyBySharepointKey(string sharepointKey)
        {
            return _revDictionary[sharepointKey];
        }

        public static String SharepointKeyByCacheKey(string cacheKey)
        {
            return _dictionary[cacheKey];
        }
    }
}