using System;
using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class DeviceParameterConverter : IListItemConverter<DeviceParameter>
    {
        private readonly IEnumerable<SPTerm> _deviceParameterTerms;
        private readonly IEnumerable<SPTerm> _names;

        public DeviceParameterConverter(IEnumerable<SPTerm> deviceParameterTerms, IEnumerable<SPTerm> names)
        {
            _deviceParameterTerms = deviceParameterTerms;
            _names = names;
        }

        public DeviceParameter Convert(ListItem listItem)
        {
            var name = listItem.Get<TaxonomyFieldValue>(SharepointFields.Parameter).ToSPTerm(_deviceParameterTerms);

            var device = listItem.Get<TaxonomyFieldValue>(SharepointFields.Device).ToSPTerm(_names);

            var valueField = Helpers.IsCultureEng ? SharepointFields.EngValue : SharepointFields.Title;

            var value = listItem.Get<String>(valueField);

            var deviceParameter = new DeviceParameter
            {
                Id = listItem.Id,
                Name = name,
                Device = device,
                Value = value
            };

            return deviceParameter;
        }
    }
}