using System;
using NetPing_modern.Services.Confluence;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using System.Text;

namespace NetPing.DAL
{
    internal static class ConfluenceDataHelper
    {
        private static readonly Logger Log = LogManager.GetLogger(LogNames.Storage);

        public static String GetContentByUrl(this IConfluenceClient client, String url)
        {
            Int32? contentID = null;

            if (url != null)
            {
                contentID = client.GetContentIdFromUrl(url);
            }

            var content = String.Empty;

            if (contentID.HasValue)
            {
                var contentTask = client.GetContenAsync(contentID.Value);
                content = contentTask.Result;
            }
            //Save file localy and change src link
            var newContent = CopyAttachments(content);
            return newContent;
        }

        public static String GetTitleByUrl(this IConfluenceClient client, String url)
        {
            Int32? contentID = null;

            if (url != null)
            {
                contentID = client.GetContentIdFromUrl(url);
            }

            var content = String.Empty;

            if (contentID.HasValue)
            {
                var contentTask = client.GetContentTitleAsync(contentID.Value);
                content = contentTask.Result;
            }

            return content;
        }

        private static string CopyAttachments(string content)
        {
            var newContent = new StringBuilder(content);

            var urls = GelListUrlFromContent(content);
            if (urls.Count > 0)
            {
                foreach (var url in urls)
                {
                    var newFileName = SaveFileFromUrlToLocal(url, UrlBuilder.LocalPath_blogFiles);
                    var newUrl = url.Remove(url.IndexOf('?'));

                    newContent = newContent.Replace(newUrl, UrlBuilder.GetblogFilesUrlUrl() + newFileName);
                }
            }
            return newContent.ToString();
        }

        private static string SaveFileFromUrlToLocal(string url, string path)
        {
            Uri uri = new Uri(url);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            using (WebClient client = new WebClient())
            {
                int counter = 0;
                while (File.Exists(path + filename))
                {
                    counter++;
                    filename = System.IO.Path.GetFileNameWithoutExtension(uri.LocalPath) + counter.ToString() + System.IO.Path.GetExtension(uri.LocalPath);
                }
                client.DownloadFile("https://netping.atlassian.net" + uri.AbsolutePath, path + filename);
                return filename;
            }
        }

        private static List<string> GelListUrlFromContent(string content)
        {
            var linkParser = new Regex(@"\b(https://netping.atlassian.net/wiki/download/)\S+\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var matches = linkParser.Matches(content);
            List<string> urls = new List<string>();
            foreach (var match in matches)
            {
                urls.Add(match.ToString());
            }
            return urls;
        }
    }
}