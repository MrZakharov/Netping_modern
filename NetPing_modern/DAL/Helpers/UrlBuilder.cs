using System;

namespace NetPing.DAL
{
    internal static class UrlBuilder
    {
        public static readonly Uri SiteRoot = new Uri("http://www.netping.ru");
        public static readonly Uri SiteSPRoot = new Uri("https://netpingeastcoltd.sharepoint.com:443");

        //   public static readonly Uri SiteRootLocal = new Uri("/");

        private static readonly Uri _products = new Uri("products/", UriKind.Relative);
        private static readonly Uri _pubFiles = new Uri("Pub/Pub/", UriKind.Relative);
        private static readonly Uri _photos = new Uri("Pub/Photos/", UriKind.Relative);
        private static readonly Uri _blog = new Uri("Blog/", UriKind.Relative);
        private static readonly Uri _firmwares = new Uri("Pub/Firmwares/", UriKind.Relative);
        private static readonly Uri _userGuide = new Uri("UserGuide/", UriKind.Relative);

        private static readonly Uri _productsUrl = new Uri(SiteRoot, _products);
        private static readonly Uri _pubFilesUrl = new Uri(SiteRoot, _pubFiles);
        private static readonly Uri _photosUrl = new Uri(SiteRoot, _photos);
        private static readonly Uri _firmwaresUrl = new Uri(SiteRoot, _firmwares);
        private static readonly Uri _blogUrl = new Uri(SiteRoot, _blog);
        private static readonly Uri _userGuideUrl = new Uri(SiteRoot, _userGuide);

        private static string _appPath = System.Web.HttpRuntime.AppDomainAppPath;

        public static string LocalPath_pubfiles = (_appPath + _pubFiles.ToString().Replace("/", "\\"));
        public static string LocalPath_photos = (_appPath + _photos.ToString().Replace("/", "\\"));
        public static string LocalPath_firmwares = (_appPath + _firmwares.ToString().Replace("/", "\\"));

        public static Uri GetSPFullUrl(string fileref)
        {
            var fullUrl = new Uri(SiteSPRoot, fileref);

            return fullUrl;
        }


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

        public static Uri GetFirmwaresUrl(String fileName)
        {
            var fileUrl = new Uri(_firmwaresUrl, fileName);

            return fileUrl;
        }

        public static String GetRelativePostUrl(String postName)
        {
            var fileUrl = new Uri(_blogUrl, postName);

            return fileUrl.PathAndQuery;
        }

        public static String GetRelativeDeviceGuideUrl(String guideName)
        {
            var fileUrl = new Uri(_userGuideUrl, guideName);

            return fileUrl.PathAndQuery;
        }
    }
}