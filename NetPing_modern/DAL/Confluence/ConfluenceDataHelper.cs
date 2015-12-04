using System;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal static class ConfluenceDataHelper
    {
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

            return content;
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
    }
}