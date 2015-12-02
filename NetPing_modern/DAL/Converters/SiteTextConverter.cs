using System;
using Microsoft.SharePoint.Client;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class SiteTextConverter : IListItemConverter<SiteText>
    {
        private readonly ConfluenceClient _confluenceClient;

        public SiteTextConverter(ConfluenceClient confluenceClient)
        {
            _confluenceClient = confluenceClient;
        }

        public SiteText Convert(ListItem listItem)
        {
            var link = listItem.Get<FieldUrlValue>(SharepointFields.BodyLink);

            var content = link == null ? String.Empty : _confluenceClient.GetContentByUrl(link.Url);

            var siteText = new SiteText
            {
                Tag = listItem.Get<String>(SharepointFields.Title),
                Text = content.ReplaceInternalLinks()
            };

            return siteText;
        }
    }
}