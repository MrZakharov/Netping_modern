using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class PostConverter : IListItemConverter<Post>
    {
        private readonly Regex tagRegex = new Regex("\\[.*\\]");

        private readonly IConfluenceClient _confluenceClient;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _categories;

        public PostConverter(IConfluenceClient confluenceClient, IEnumerable<SPTerm> names, IEnumerable<SPTerm> categories)
        {
            _confluenceClient = confluenceClient;
            _names = names;
            _categories = categories;
        }

        public Post Convert(ListItem listItem)
        {
            var link = listItem["Body_link"] as FieldUrlValue;

            var content = link == null ? "" : _confluenceClient.GetContentByUrl(link.Url);

            var title = link == null ? "" : _confluenceClient.GetTitleByUrl(link.Url);

            var metaHtml = GetPageProperties(content);

            if (metaHtml != null)
            {
                content = RemovePagePropertiesInContent(content);
            }

            if (!String.IsNullOrWhiteSpace(title))
            {
                title = tagRegex.Replace(title, String.Empty);
            }

            var post = new Post
            {
                Id = (listItem["Old_id"] == null) ? 0 : Int32.Parse(listItem["Old_id"].ToString()),
                Title = title,
                Devices = (listItem["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(_names),
                Body = content.ReplaceInternalLinks(),
                Category = (listItem["Category"] as TaxonomyFieldValue).ToSPTerm(_categories),
                Created = (DateTime) listItem["Pub_date"],
                Url_name = "/Blog/" + (listItem["Body_link"] as FieldUrlValue).Description.Replace(".", "x2E").Trim(' '),
                IsTop = (Boolean) listItem["TOP"],
                MetaHtml = metaHtml
            };

            return post;
        }

        /// <summary>
        ///     Удаляем из страницы блок PageProperties (блок с мета тегами)
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private String RemovePagePropertiesInContent(String content)
        {
            var html = new HtmlDocument();
            html.LoadHtml(content);
            var table = GetPagePropertiesContent(html);
            html.DocumentNode.SelectSingleNode(table.XPath).Remove();
            return html.DocumentNode.InnerHtml;
        }

        /// <summary>
        ///     Находим блок PageProperties на странице
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Dictionary<String, String> GetPageProperties(String content)
        {
            var html = new HtmlDocument();
            html.LoadHtml(content);
            try
            {
                var pagePropertiesContent = GetPagePropertiesContent(html);

                var table = pagePropertiesContent?.Descendants("table").FirstOrDefault();

                if (table != null)
                {
                    var result = new Dictionary<String, String>();
                    var trNodes = table.ChildNodes[0].ChildNodes.Where(x => x.Name == "tr");
                    foreach (var tr in trNodes)
                    {
                        var tdNodes = tr.ChildNodes.Where(x => x.Name == "td").ToArray();
                        if (tdNodes.Count() == 2)
                        {
                            var key = tdNodes[0].InnerText;
                            if (!result.ContainsKey(key))
                                result.Add(tdNodes[0].InnerText, tdNodes[1].InnerText);
                        }
                    }
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        private static HtmlNode GetPagePropertiesContent(HtmlDocument html)
        {
            return html.DocumentNode.Descendants("div").FirstOrDefault(x => x.Attributes.Contains("data-macro-name")
                                                                   &&
                                                                   x.Attributes["data-macro-name"].Value.Contains(
                                                                       "details"));
        }
    }
}