using Microsoft.SharePoint.Client;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class HtmlInjectionConverter : IListItemConverter<HTMLInjection>
    {
        HTMLInjection IListItemConverter<HTMLInjection>.Convert(ListItem listItem)
        {
            var htmlInjection = new HTMLInjection
            {
                HTML = listItem["HTML"].ToString(),
                Page = listItem["Page"].ToString(),
                Section = listItem["Section"].ToString(),
                Title = listItem["Title"].ToString()
            };

            return htmlInjection;
        }
    }
}