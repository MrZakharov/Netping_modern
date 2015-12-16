using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class FirmwareFileConverter : IListItemConverter<SFile>
    {
        private readonly SharepointClient _sharepointClient;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _fileTypeTerms;

        public FirmwareFileConverter(SharepointClient sharepointClient, IEnumerable<SPTerm> names, IEnumerable<SPTerm> fileTypeTerms)
        {
            _sharepointClient = sharepointClient;
            _names = names;
            _fileTypeTerms = fileTypeTerms;
        }

        public SFile Convert(ListItem listItem)
        {
            var firmwareFileType = "4dadfd09-f883-4f42-9178-ded2fe88016b";
            var firmwareFileType2 = "e3de2072-1eb2-4b6d-a7e2-3319bf89836d";

            var fileType = _fileTypeTerms.FirstOrDefault(t => t.Id == new Guid(firmwareFileType));

            var docType = listItem.Get<TaxonomyFieldValue>(SharepointFields.DocType);
            
            if (docType.TermGuid == firmwareFileType2)
            {
                fileType = _fileTypeTerms.FirstOrDefault(t => t.Id == new Guid(firmwareFileType2));
            }

            var name = listItem.Get<String>(SharepointFields.FileLeaf);
            var title = listItem.Get<String>(SharepointFields.Title);
            var devices = listItem.Get<TaxonomyFieldValueCollection>(SharepointFields.Devices).ToSPTermList(_names);
            var createDate = listItem.Get<DateTime>(SharepointFields.Created);
            var url = UrlBuilder.GetFirmwaresUrl(name).ToString();

            var firmwareFile = new SFile
            {
                Id = listItem.Id,
                Name = name,
                Title = title,
                Devices = devices,
                File_type = fileType,
                Created = createDate,
                Url = url
            };

            return firmwareFile;
        }
    }
}