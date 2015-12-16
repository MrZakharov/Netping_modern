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
            var pictureName = listItem.Get<String>(SharepointFields.FileLeaf);

            var deviceName = listItem.Get<TaxonomyFieldValue>(SharepointFields.Device).ToSPTerm(_names);

            if (!String.IsNullOrEmpty(pictureName))
            {
                pictureName = pictureName.Replace(" ", String.Empty);
            }

            var photosUrl = UrlBuilder.GetPhotosUrl(pictureName).ToString();

            var isBigPhoto = IsBigPhoto(pictureName);

            var isCover = listItem.Get<Boolean>(SharepointFields.Cover);

            var devicePhoto = new DevicePhoto
            {
                Name = pictureName,
                Dev_name = deviceName,
                Url = photosUrl,
                IsBig = isBigPhoto,
                IsCover = isCover
            };

            return devicePhoto;
        }

        private Boolean IsBigPhoto(String pictureName)
        {
            return pictureName?.Contains("big") ?? false;
        }
    }
}