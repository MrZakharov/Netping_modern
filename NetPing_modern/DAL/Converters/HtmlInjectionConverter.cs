using System;
using Microsoft.SharePoint.Client;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class HtmlInjectionConverter : IListItemConverter<HTMLInjection>
    {
        HTMLInjection IListItemConverter<HTMLInjection>.Convert(ListItem listItem, SharepointClient sp)
        {
            var html = listItem.Get<String>(SharepointFields.Html);
            var page = listItem.Get<String>(SharepointFields.Page);
            var section = listItem.Get<String>(SharepointFields.Section);
            var title = listItem.Get<String>(SharepointFields.Title);

            var htmlInjection = new HTMLInjection
            {
                HTML = html,
                Page = page,
                Section = section,
                Title = title
            };

            return htmlInjection;
        }
    }
}