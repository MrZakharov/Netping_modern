using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.DAL.Model;
using NetPing_modern.Services.Confluence;
using NLog;

namespace NetPing.DAL
{
    internal class DeviceManualFileConverter : IListItemConverter<SFile>
    {
        private readonly IConfluenceClient _confluenceClient;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _fileTypeTerms;
        private readonly Action<UserManualModel> _manualSaver;

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Loader);

        public DeviceManualFileConverter(IConfluenceClient confluenceClient, IEnumerable<SPTerm> names, IEnumerable<SPTerm> fileTypeTerms, Action<UserManualModel> manualSaver)
        {
            _confluenceClient = confluenceClient;
            _names = names;
            _fileTypeTerms = fileTypeTerms;
            _manualSaver = manualSaver;
        }

        public SFile Convert(ListItem listItem, SharepointClient sp)
        {
            try
            {
                var userGuideFileName = "User guide";

                var fileName = listItem.Get<String>(SharepointFields.Title);

                var fileType = listItem.Get<TaxonomyFieldValue>(SharepointFields.ManualFileType).ToSPTerm(_fileTypeTerms);

                var title = listItem.Get<String>(SharepointFields.Title);

                var devices = listItem.Get<TaxonomyFieldValueCollection>(SharepointFields.Devices).ToSPTermList(_names);

                var created = listItem.Get<DateTime>(SharepointFields.Created);

                var fileUrl = listItem.Get<FieldUrlValue>(SharepointFields.UrlUpperCase).Url;

                Debug.WriteLine($"Start loading manual '{fileUrl}'");
                Log.Trace($"Start loading manual '{fileUrl}'");

                var sFile = new SFile
                {
                    Id = listItem.Id,
                    Name = fileName,
                    Title = title,
                    Devices = devices,
                    File_type = fileType,
                    Created = created,
                    Url = UrlBuilder.GetPublicFilesUrl(fileName).ToString()
            };
                if (fileType.OwnNameFromPath != userGuideFileName)
                {
                   sp.DownloadFileToLocal(fileUrl, UrlBuilder.LocalPath_pubfiles, fileName);
                }
                else
                {
                    sFile.Url = fileUrl;
                }
                /*
                if (fileType.OwnNameFromPath == userGuideFileName)
            {
                var contentId = _confluenceClient.GetContentIdFromUrl(fileUrl);

                if (contentId.HasValue)
                {
                    var content = _confluenceClient.GetUserManual(contentId.Value, listItem.Id);

                    _manualSaver(content);

                    var url = UrlBuilder.GetRelativeDeviceGuideUrl(PrepareUrlName(content)).ToString();

                    sFile.Url = url;
                }
                else
                {
                    Debug.WriteLine($"End loading manual '{fileUrl}'");
                    Log.Trace($"End loading manual '{fileUrl}'");

                    return null;
                }


            } else
                {
                   
                    sp.DownloadFileToLocal(fileUrl, UrlBuilder.LocalPath_pubfiles, fileName);
                }
*/
                Debug.WriteLine($"End loading manual '{fileUrl}'");
                Log.Trace($"End loading manual '{fileUrl}'");

                return sFile;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private String PrepareUrlName(UserManualModel content)
        {
            return content.Title.Replace("/", "");
        }
    }
}