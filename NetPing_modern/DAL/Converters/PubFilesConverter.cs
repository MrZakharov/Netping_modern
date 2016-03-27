using System;
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

        public PubFiles Convert(ListItem listItem, SharepointClient sp)
        {
            var fileName = listItem.Get<String>(SharepointFields.FileLeaf);

            var fileUrl = UrlBuilder.GetPublicFilesUrl(fileName);

            var fileType = listItem.Get<TaxonomyFieldValue>(SharepointFields.FileType).ToSPTerm(_fileTypeTerms);

            var urlLink = listItem.Get<FieldUrlValue>(SharepointFields.Url).Url;

            var pubFiles = new PubFiles
            {
                Name = fileName,
                File_type = fileType,
                Url = fileUrl.ToString(),
                Url_link = urlLink
            };

            var fileRef = listItem.Get<String>(SharepointFields.FileRef);
            sp.DownloadFileToLocal(fileRef, UrlBuilder.LocalPath_pubfiles,fileName);


            return pubFiles;
        }
    }
}