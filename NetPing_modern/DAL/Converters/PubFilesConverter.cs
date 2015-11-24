using System.Collections.Generic;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;

namespace NetPing.DAL
{
    internal class PubFilesConverter : IListItemConverter<PubFiles>
    {
        private readonly IEnumerable<SPTerm> _fileTypeTerms;

        public PubFilesConverter(IEnumerable<SPTerm> fileTypeTerms)
        {
            _fileTypeTerms = fileTypeTerms;
        }

        public PubFiles Convert(ListItem listItem)
        {
            var filesPath = "http://netping.ru/Pub/Pub/";

            var pubFiles = new PubFiles
            {
                Name = listItem["FileLeafRef"].ToString(),
                File_type = (listItem["File_type"] as TaxonomyFieldValue).ToSPTerm(_fileTypeTerms),
                Url = filesPath + listItem["FileLeafRef"],
                Url_link = (listItem["Url"] as FieldUrlValue)?.Url
            };

            return pubFiles;
        }
    }
}