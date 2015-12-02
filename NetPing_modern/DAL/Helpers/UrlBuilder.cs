using System;

namespace NetPing.DAL
{
    internal static class UrlBuilder
    {
        public static readonly Uri SiteRoot = new Uri("http://www.netping.ru");

        private static readonly Uri _products = new Uri("products", UriKind.Relative);
        private static readonly Uri _pubFiles = new Uri("Pub/Pub", UriKind.Relative);
        private static readonly Uri _photos = new Uri("Pub/Photos", UriKind.Relative);

        private static readonly Uri _productsUrl =new Uri(SiteRoot, _products);
        private static readonly Uri _pubFilesUrl = new Uri(SiteRoot, _pubFiles);
        private static readonly Uri _photosUrl = new Uri(SiteRoot, _photos);

        public static Uri GetDeviceUrl(String deviceName)
        {
            var deviceUrl = new Uri(_productsUrl, deviceName);

            return deviceUrl;
        }

        public static Uri GetPublicFilesUrl(String fileName)
        {
            var fileUrl = new Uri(_pubFilesUrl, fileName);

            return fileUrl;
        }

        public static Uri GetPhotosUrl(String fileName)
        {
            var fileUrl = new Uri(_photosUrl, fileName);

            return fileUrl;
        }
    }
}