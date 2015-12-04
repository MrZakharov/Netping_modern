using System;
using System.Collections.Generic;

namespace NetPing.DAL
{
    internal static class SharepointToCacheNamesMapper
    {
        private static readonly Dictionary<string, string> Dictionary = new Dictionary<string, string>();
        private static readonly Dictionary<string, string> RevDictionary = new Dictionary<string, string>();

        static SharepointToCacheNamesMapper()
        {
            Dictionary.Add(CacheKeys.Names, SharepointKeys.Names);
            Dictionary.Add(CacheKeys.Purposes, SharepointKeys.Purposes);
            Dictionary.Add(CacheKeys.DeviceParameterNames, SharepointKeys.DeviceParameterNames);
            Dictionary.Add(CacheKeys.Labels, SharepointKeys.Labels);
            Dictionary.Add(CacheKeys.PostCategories, SharepointKeys.PostCategories);
            Dictionary.Add(CacheKeys.DocumentTypes, SharepointKeys.DocumentTypes);
            Dictionary.Add(CacheKeys.HtmlInjection, SharepointKeys.HtmlInjection);
            Dictionary.Add(CacheKeys.SiteTexts, SharepointKeys.SiteTexts);
            Dictionary.Add(CacheKeys.DevicePhotos, SharepointKeys.DevicePhotos);
            Dictionary.Add(CacheKeys.DeviceParameters, SharepointKeys.DeviceParameters);
            Dictionary.Add(CacheKeys.PubFiles, SharepointKeys.PubFiles);
            Dictionary.Add(CacheKeys.Posts, SharepointKeys.Posts);
            Dictionary.Add(CacheKeys.Devices, SharepointKeys.Devices);
            Dictionary.Add(CacheKeys.SFiles, SharepointKeys.SFiles);

            foreach (var item in Dictionary)
            {
                RevDictionary.Add(item.Value, item.Key);
            }
        }

        public static String CacheKeyBySharepointKey(string sharepointKey)
        {
            return RevDictionary[sharepointKey];
        }

        public static String SharepointKeyByCacheKey(string cacheKey)
        {
            return Dictionary[cacheKey];
        }
    }
}