using System;

namespace NetPing.DAL
{
    internal static class UrlBuilder
    {
        public static readonly Uri SiteRoot = new Uri("http://www.netping.ru");

        private static readonly Uri _products = new Uri("products", UriKind.Relative);

        private static readonly Uri _productsUrl =new Uri(SiteRoot, _products);

        public static Uri GetDeviceUrl(String devicePath)
        {
            var deviceUrl = new Uri(_productsUrl, devicePath);

            return deviceUrl;
        }
    }
}