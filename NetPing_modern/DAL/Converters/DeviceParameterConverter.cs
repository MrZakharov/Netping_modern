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
            var deviceParameter = new DeviceParameter
            {
                Id = listItem.Id,
                Name = (listItem["Parameter"] as TaxonomyFieldValue).ToSPTerm(_deviceParameterTerms),
                Device = (listItem["Device"] as TaxonomyFieldValue).ToSPTerm(_names),
                Value = (Helpers.IsCultureEng) ? listItem["ENG_value"].ToString() : listItem["Title"].ToString()
            };

            return deviceParameter;
        }
    }
}