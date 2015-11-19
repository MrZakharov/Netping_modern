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
            var folder = _sharepointClient.GetFolderParent(listItem["FileDirRef"].ToString());

            var fileType = _fileTypeTerms.FirstOrDefault(t => t.Id == new Guid("4dadfd09-f883-4f42-9178-ded2fe88016b"));

            if ((listItem["DocType"] as TaxonomyFieldValue).TermGuid == "e3de2072-1eb2-4b6d-a7e2-3319bf89836d")
            {
                fileType =
                    _fileTypeTerms.FirstOrDefault(t => t.Id == new Guid("e3de2072-1eb2-4b6d-a7e2-3319bf89836d"));
            }

            var firmwareFile = new SFile
            {
                Id = listItem.Id,
                Name = listItem["FileLeafRef"] as String,
                Title = listItem["Title"] as String,
                Devices = (folder["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(_names),
                File_type = fileType,
                Created = (DateTime)listItem["Created"],
                Url = "http://netping.ru/Pub/Firmwares/" + (listItem["FileLeafRef"] as String)
            };

            return firmwareFile;
        }
    }
}