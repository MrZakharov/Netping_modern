using System;
using NetPing_modern.Services.Confluence;
using System.IO;
using System.Net;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NLog;
using System.Text;
using HtmlAgilityPack;
using System.Linq;

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

        private static string CopyAttachments(string content)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(content);

            //process all nodes
            var nodes = doc.DocumentNode.Descendants().Where(d => d.Attributes.Where( a => a.Value.Contains("https://netping.atlassian.net/wiki/download/")).Count() > 0);

            foreach (var node in nodes)
            {
                var attributesWithFullUri = node.Attributes.Where(a => a.Value.Contains("https://netping.atlassian.net/wiki/download/"));
                foreach (var attribute in attributesWithFullUri)
                {
                    if (attribute.Value.Contains("https://netping.atlassian.net/wiki/download/"))
                    {
                        string oldFileName;
                        var newFileName = SaveFileFromUrlToLocal(attribute.Value, UrlBuilder.LocalPath_blogTempFiles, out oldFileName);
                        var oldUrl = attribute.Value.Remove(attribute.Value.IndexOf('?'));
                        attribute.Value = attribute.Value.Replace(oldUrl, UrlBuilder.GetblogFilesUrl() + newFileName);

                        ChangeLinksToDownloadedFileFromNode(node, oldFileName, newFileName, oldUrl);
                    }
                }

                if(node.Attributes["data-base-url"] != null && node.Attributes["data-base-url"].Value == "https://netping.atlassian.net/wiki")
                {
                    node.Attributes["data-base-url"].Value = UrlBuilder.GetblogFilesUrl().ToString();
                }
            }

            //change srcset attribute (workaround)
            nodes = doc.DocumentNode.Descendants().Where(d => d.Attributes.Any(a => a.Name == "srcset") && d.Attributes.Any(a => a.Name == "src") && d.Attributes.Any(a => a.Name == "data-image-src"));
            foreach (var node in nodes)
            {
                if(node.Attributes["src"] != null && node.Attributes["src"].Value.StartsWith(UrlBuilder.GetblogFilesUrl().ToString())
                    && node.Attributes["data-image-src"] != null && node.Attributes["data-image-src"].Value.StartsWith(UrlBuilder.GetblogFilesUrl().ToString()))
                {
                    var oldUrl = node.Attributes["src"].Value.Remove(node.Attributes["src"].Value.IndexOf('?'));
                    var newUrl = node.Attributes["data-image-src"].Value.Remove(node.Attributes["data-image-src"].Value.IndexOf('?'));

                    if (Uri.IsWellFormedUriString(oldUrl, UriKind.Absolute) && Uri.IsWellFormedUriString(newUrl, UriKind.Absolute))
                    {
                        node.Attributes["srcset"].Value = node.Attributes["srcset"].Value.Replace(oldUrl, newUrl);
                    }
                }
            }


            return doc.DocumentNode.OuterHtml;
        }

        private static void ChangeLinksToDownloadedFileFromNode(HtmlNode node, string oldFileName, string newFileName, string oldUrl)
        {
            //rewrite the atrributes which have wiki/download/ to https://netping.atlassian.net/wiki/download/
            //solve srcset issue

            var wikiDownloadsAttributes = node.Attributes.Where(a => a.Name == "srcset" && a.Value.Contains("/wiki/download/") && a.Value.Contains(oldFileName));
            foreach (var attribute in wikiDownloadsAttributes)
            {
                attribute.Value = attribute.Value.Replace("/wiki/download/", "https://netping.atlassian.net/wiki/download/");
            }

            var theSameFileNameAttributes = node.Attributes.Where(a => a.Value.Contains("https://netping.atlassian.net/wiki/download/") && a.Value.Contains(oldFileName));

            //rewrite the atrributes with the same file name
            foreach (var attribute in theSameFileNameAttributes)
            {
                if (attribute.Value.Contains(oldUrl))
                {
                    attribute.Value = attribute.Value.Replace(oldUrl, UrlBuilder.GetblogFilesUrl() + newFileName);
                }
            }
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

        private static string SaveFileFromUrlToLocal(string url, string path, out string oldFileName)
        {
            Uri uri = new Uri(url);
            string filename = System.IO.Path.GetFileName(uri.LocalPath);

            oldFileName = filename;

            using (WebClient client = new WebClient())
            {
                int counter = 0;
                while (File.Exists(Path.Combine(path, filename)))
                {
                    counter++;
                    filename = Path.GetFileNameWithoutExtension(uri.LocalPath) + counter.ToString() + Path.GetExtension(uri.LocalPath);
                }
                client.DownloadFile("https://netping.atlassian.net" + uri.AbsolutePath, Path.Combine(path, filename));
                return filename;
            }
        }
    }
}