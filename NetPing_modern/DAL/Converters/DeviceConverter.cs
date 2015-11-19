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
        private readonly Regex ConfluenceImgSrcRegex = new Regex(@"\ssrc=""/wiki(?<src>[^\""]+)""");

        private readonly Regex ConfluenceDataBaseUrlRegex = new Regex(@"\sdata-base-url=""(?<src>[^\""]+)""");

        private readonly ConfluenceClient _confluenceClient;
        private readonly Dictionary<string, string> _dataTable;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _purposes;
        private readonly IEnumerable<SPTerm> _labels;
        private readonly DateTime _deviceStockUpdate;

        public DeviceConverter(ConfluenceClient confluenceClient, Dictionary<String, String> dataTable, IEnumerable<SPTerm> names, IEnumerable<SPTerm> purposes, IEnumerable<SPTerm> labels, DateTime deviceStockUpdate)
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
            var guidID = listItem["_x0031_C_ref"] as String;

            var _stock = guidID != null && _dataTable.ContainsKey(guidID) ? _dataTable[guidID] : "-1";

            var device = new Device
            {
                Id = listItem.Id,
                OldKey = listItem["Title"] as String,
                Name = (listItem["Name"] as TaxonomyFieldValue).ToSPTerm(_names),
                Destination = (listItem["Destination"] as TaxonomyFieldValueCollection).ToSPTermList(_purposes),
                Connected_devices = (listItem["Connected_devices"] as TaxonomyFieldValueCollection).ToSPTermList(_names),
                Price = listItem["Price"] as Double?,
                Label = (listItem["Label"] as TaxonomyFieldValue).ToSPTerm(_labels),
                Created = (DateTime)listItem["Created"],
                Url = listItem["Url"] as String,
                DeviceStockUpdate = _deviceStockUpdate
                ,
                DeviceStock = Int16.Parse(_stock)
            };


            var urlField = listItem["Short_descr"] as FieldUrlValue;

            if (urlField != null)
            {
                LoadFromConfluence(device, d => d.Short_description, urlField.Url);
            }

            urlField = listItem["Long_descr"] as FieldUrlValue;

            if (urlField != null)
            {
                LoadFromConfluence(device, d => d.Long_description, urlField.Url);
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
                var propertyValue = ReplaceConfluenceImages(StylishHeaders3(CleanSpanStyles(CleanFonts((content)))));
                propertyInfo.SetValue(device, propertyValue);
            }

            if (propertyInfo.GetValue(device) == null)
            {
                propertyInfo.SetValue(device, String.Empty);
            }
        }


        private String CleanSpanStyles(String str)
        {
            for (var tagstart = str.IndexOf("<span"); tagstart != -1; tagstart = str.IndexOf("<span", tagstart + 1))
            {
                var tagend = str.IndexOf('>', tagstart);
                var tag = str.Substring(tagstart, tagend - tagstart + 1);
                if (tag.Contains("style"))
                {
                    str = str.Remove(
                        tag.IndexOf("style") + tagstart,
                        tag.LastIndexOf('"') - tag.IndexOf("style") + 1);
                }
            }
            return str;
        }

        private String CleanFonts(String str)
        {
            for (var tagstart = str.IndexOf("<font"); tagstart != -1; tagstart = str.IndexOf("<font", tagstart + 1))
            {
                var tagend = str.IndexOf('>', tagstart);
                var tag = str.Substring(tagstart, tagend - tagstart + 1);
                if (tag.Contains("color"))
                {
                    str = str.Remove(
                        tag.IndexOf("color") + tagstart,
                        tag.LastIndexOf('"') - tag.IndexOf("color") + 1);
                }
            }
            return str;
        }

        private String StylishHeaders3(String str)
        {
            //str = str.Replace("<h3", "<h3 class=\"shutter collapsed\"><span class=dashed>");
            //str = str.Replace("</h3>", "</span></h3>");
            return str;
        }

        private String ReplaceConfluenceImages(String str)
        {
            return ConfluenceImageTagRegex.Replace(str, ConfluenceImage);
        }

        private String ConfluenceImage(Match match)
        {
            var s = match.ToString();
            if (s.Contains("confluence-embedded-image"))
            {
                var src = ConfluenceImgSrcRegex.Match(s);
                var baseUrl = ConfluenceDataBaseUrlRegex.Match(s);
                if (src.Success && baseUrl.Success)
                {
                    return ConfluenceImgSrcRegex.Replace(s,
                        String.Format(" src=\"{0}\"", baseUrl.Groups["src"].Value + src.Groups["src"].Value));
                }
            }
            return s;
        }

        private readonly Regex ConfluenceImageTagRegex = new Regex(@"\<img [^\>]+\>", RegexOptions.IgnoreCase);
    }
}