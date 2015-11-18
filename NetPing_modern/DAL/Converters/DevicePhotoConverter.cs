using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class DevicePhotoConverter : IListItemConverter<DevicePhoto>
    {
        private readonly IEnumerable<SPTerm> _names;

        public DevicePhotoConverter(IEnumerable<SPTerm> names)
        {
            _names = names;
        }

        public DevicePhoto Convert(ListItem listItem)
        {
            var pictureUrl = listItem["FileLeafRef"].ToString();

            if (!String.IsNullOrEmpty(pictureUrl))
            {
                pictureUrl = pictureUrl.Replace(" ", String.Empty);
            }

            var photosPath = "http://www.netping.ru/Pub/Photos/";

            var devicePhoto = new DevicePhoto
            {
                Name = listItem["FileLeafRef"].ToString(),
                Dev_name = (listItem["Device"] as TaxonomyFieldValue).ToSPTerm(_names),
                Url = photosPath + pictureUrl,
                IsBig = pictureUrl.Contains("big") ? true : false,
                IsCover = System.Convert.ToBoolean(listItem["Cover"])
            };

            return devicePhoto;
        }
    }
}