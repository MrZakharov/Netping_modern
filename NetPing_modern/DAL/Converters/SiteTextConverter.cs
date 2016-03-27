using System;
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

        public SiteText Convert(ListItem listItem, SharepointClient sp)
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