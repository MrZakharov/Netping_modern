using Microsoft.SharePoint.Client;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class SiteTextConverter : IListItemConverter<SiteText>
    {
        private readonly IConfluenceClient _confluenceClient;

        public SiteTextConverter(IConfluenceClient confluenceClient)
        {
            _confluenceClient = confluenceClient;
        }

        public SiteText Convert(ListItem listItem)
        {
            var link = listItem["Body_link"] as FieldUrlValue;

            var content = link == null ? "" : _confluenceClient.GetContentByUrl(link.Url);

            var siteText = new SiteText
            {
                Tag = listItem["Title"].ToString(),
                Text = content.ReplaceInternalLinks()
            };

            return siteText;
        }
    }
}