using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class DeviceConverter : IListItemConverter<Device>
    {
        private readonly IConfluenceClient _confluenceClient;
        private readonly Dictionary<String, String> _dataTable;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _purposes;
        private readonly IEnumerable<SPTerm> _labels;
        private readonly DateTime _deviceStockUpdate;

        public DeviceConverter(
            IConfluenceClient confluenceClient, 
            Dictionary<String, String> dataTable, 
            IEnumerable<SPTerm> names, 
            IEnumerable<SPTerm> purposes, 
            IEnumerable<SPTerm> labels, 
            DateTime deviceStockUpdate)
        {
            _confluenceClient = confluenceClient;
            _dataTable = dataTable;
            _names = names;
            _purposes = purposes;
            _labels = labels;
            _deviceStockUpdate = deviceStockUpdate;
        }

        public Device Convert(ListItem listItem)
        {
            var guidID = listItem.Get<String>(SharepointFields.DeviceStockID);

            var emptyStockNumber = "-1";

            var stock = guidID != null && _dataTable.ContainsKey(guidID) ? _dataTable[guidID] : emptyStockNumber;

            var stockNumber = Int16.Parse(stock);

            var title = listItem.Get<String>(SharepointFields.Title);

            var name = listItem.Get<TaxonomyFieldValue>(SharepointFields.Name).ToSPTerm(_names);

            var purposes = listItem.Get<TaxonomyFieldValue>(SharepointFields.Purpose).ToSPTerm(_purposes);

            var linkedDevices =
                listItem.Get<TaxonomyFieldValueCollection>(SharepointFields.LinkedDevices).ToSPTermList(_names);

            var price = listItem.Get<Double?>(SharepointFields.Price);

            var label = listItem.Get<TaxonomyFieldValue>(SharepointFields.Label).ToSPTerm(_labels);

            var created = listItem.Get<DateTime>(SharepointFields.Created);

            var url = listItem.Get<String>(SharepointFields.Url);

            var device = new Device
            {
                Id = listItem.Id,
                OldKey = title,
                Name = name,
                Destination = purposes,
                Connected_devices = linkedDevices,
                Price = price,
                Label = label,
                Created = created,
                Url = url,
                DeviceStockUpdate = _deviceStockUpdate,
                DeviceStock = stockNumber
            };

            var shortDescriptionField = listItem.Get<FieldUrlValue>(SharepointFields.ShortDescription);

            if (shortDescriptionField != null)
            {
                LoadFromConfluence(device, d => d.Short_description, shortDescriptionField.Url);
            }

            var longDescriptionField = listItem.Get<FieldUrlValue>(SharepointFields.LongDescription);

            if (longDescriptionField != null)
            {
                LoadFromConfluence(device, d => d.Long_description, longDescriptionField.Url);
            }

            return device;
        }

        private void LoadFromConfluence(Device device, Expression<Func<Device, String>> expression, String url)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<Device>());

            var propertyInfo = typeof(Device).GetProperty(meta.PropertyName);

            var id = _confluenceClient.GetContentIdFromUrl(url);

            if (id == null)
            {
                propertyInfo.SetValue(device, String.Empty);

                return;
            }

            var contentTask = _confluenceClient.GetContenAsync((Int32)id);

            var content = contentTask.Result;

            if (!String.IsNullOrWhiteSpace(content))
            {
                var preparedContent = DescriptionContentConverter.Convert(content);

                propertyInfo.SetValue(device, preparedContent);
            }

            if (propertyInfo.GetValue(device) == null)
            {
                propertyInfo.SetValue(device, String.Empty);
            }
        }

        private class DescriptionContentConverter
        {
            private static readonly Regex ConfluenceImgSrcRegex = new Regex(@"\ssrc=""/wiki(?<src>[^\""]+)""");

            private static readonly Regex ConfluenceDataBaseUrlRegex = new Regex(@"\sdata-base-url=""(?<src>[^\""]+)""");

            private static readonly Regex ConfluenceImageTagRegex = new Regex(@"\<img [^\>]+\>", RegexOptions.IgnoreCase);

            public static String Convert(String content)
            {
                return ReplaceConfluenceImages(
                            RemoveSpanTagsWithStyle(
                                RemoveFontTagsWithColor(content)));
            }

            private static String RemoveFontTagsWithColor(String content)
            {
                return RemoveTagWithAttribute(content, "font", "color");
            }

            private static String RemoveSpanTagsWithStyle(String content)
            {
                return RemoveTagWithAttribute(content, "span", "style");
            }

            private static String RemoveTagWithAttribute(String content, String tagName, String attributeName)
            {
                var removedTagStart = $"<{tagName}";
                var removedTagEnd = '>';

                for (var tagStart = content.IndexOf(removedTagStart, StringComparison.Ordinal);
                    tagStart != -1;
                    tagStart = content.IndexOf(removedTagStart, tagStart + 1, StringComparison.Ordinal))
                {
                    var tagEnd = content.IndexOf(removedTagEnd, tagStart);

                    var tag = content.Substring(tagStart, tagEnd - tagStart + 1);

                    if (tag.Contains(attributeName))
                    {
                        content = content.Remove(
                            tag.IndexOf(attributeName, StringComparison.Ordinal) + tagStart,
                            tag.LastIndexOf('"') - tag.IndexOf(attributeName, StringComparison.Ordinal) + 1);
                    }
                }

                return content;
            }

            private static String ReplaceConfluenceImages(String content)
            {
                return ConfluenceImageTagRegex.Replace(content, ConfluenceImageEvaluator);
            }

            private static String ConfluenceImageEvaluator(Match match)
            {
                var matchToken = match.ToString();

                if (matchToken.Contains("confluence-embedded-image"))
                {
                    var src = ConfluenceImgSrcRegex.Match(matchToken);

                    var baseUrl = ConfluenceDataBaseUrlRegex.Match(matchToken);

                    if (src.Success && baseUrl.Success)
                    {
                        var replacement = $" src=\"{baseUrl.Groups["src"].Value + src.Groups["src"].Value}\"";

                        return ConfluenceImgSrcRegex.Replace(matchToken, replacement);
                    }
                }

                return matchToken;
            }
        }
    }
}