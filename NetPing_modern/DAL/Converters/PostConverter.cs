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
        private readonly Regex _tagRegex = new Regex("\\[.*\\]");

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
            var link = listItem.Get<FieldUrlValue>(SharepointFields.BodyLink);

            var content = link == null ? String.Empty : _confluenceClient.GetContentByUrl(link.Url);

            var title = link == null ? String.Empty : _confluenceClient.GetTitleByUrl(link.Url);

            var metaHtml = GetPageProperties(content);

            if (metaHtml != null)
            {
                content = RemovePagePropertiesInContent(content);
            }

            if (!String.IsNullOrWhiteSpace(title))
            {
                title = _tagRegex.Replace(title, String.Empty);
            }

            var id = listItem.Get<Int32>(SharepointFields.OldID);

            var devices = listItem.Get<TaxonomyFieldValueCollection>(SharepointFields.Devices).ToSPTermList(_names);

            var body = content.ReplaceInternalLinks();

            var category = listItem.Get<TaxonomyFieldValue>(SharepointFields.Category).ToSPTerm(_categories);

            var createDate = listItem.Get<DateTime>(SharepointFields.PublicationDate);

            var urlName = link?.Description.Replace(".", "x2E").Trim(' ') ?? String.Empty;

            var relativeUrl = UrlBuilder.GetRelativePostUrl(urlName).ToString();

            var isTopPost = listItem.Get<Boolean>(SharepointFields.IsTop);

            var post = new Post
            {
                Id = id,
                Title = title,
                Devices = devices,
                Body = body,
                Category = category,
                Created = createDate,
                Url_name = relativeUrl,
                IsTop = isTopPost,
                MetaHtml = metaHtml
            };

            return post;
        }

        /// <summary>
        ///  Removes from content PageProperties block (block with meta tags).
        /// </summary>
        private String RemovePagePropertiesInContent(String content)
        {
            var html = new HtmlDocument();
            html.LoadHtml(content);
            var table = GetPagePropertiesContent(html);
            html.DocumentNode.SelectSingleNode(table.XPath).Remove();
            return html.DocumentNode.InnerHtml;
        }

        /// <summary>
        /// Finds PageProperties blocks in content.
        /// </summary>
        private Dictionary<String, String> GetPageProperties(String content)
        {
            var trTag = "tr";
            var tdTag = "td";
            var tableTag = "table";

            var html = new HtmlDocument();

            html.LoadHtml(content);

            try
            {
                var pagePropertiesContent = GetPagePropertiesContent(html);
                
                var table = pagePropertiesContent?.Descendants(tableTag).FirstOrDefault();

                if (table != null)
                {
                    var result = new Dictionary<String, String>();
                    
                    var trNodes = table.ChildNodes[0].ChildNodes.Where(x => x.Name == trTag);

                    foreach (var tr in trNodes)
                    {
                        var tdNodes = tr.ChildNodes.Where(x => x.Name == tdTag).ToArray();

                        if (tdNodes.Count() == 2)
                        {
                            var key = tdNodes[0].InnerText;

                            if (!result.ContainsKey(key))
                            {
                                result.Add(tdNodes[0].InnerText, tdNodes[1].InnerText);
                            }
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

        private HtmlNode GetPagePropertiesContent(HtmlDocument html)
        {
            var divTag = "div";
            var dataMacroName = "data-macro-name";
            var details = "details";

            return html.DocumentNode
                .Descendants(divTag)
                .FirstOrDefault(x => x.Attributes.Contains(dataMacroName)
                                     &&
                                     x.Attributes[dataMacroName].Value
                                         .Contains(
                                             details));
        }
    }
}