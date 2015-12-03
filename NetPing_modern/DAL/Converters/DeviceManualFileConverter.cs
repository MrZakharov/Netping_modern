using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Models;
using NetPing.Tools;
using NetPing_modern.DAL.Model;
using NetPing_modern.Services.Confluence;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class DeviceManualFileConverter : IListItemConverter<SFile>
    {
        private readonly IConfluenceClient _confluenceClient;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _fileTypeTerms;

        public DeviceManualFileConverter(IConfluenceClient confluenceClient, IEnumerable<SPTerm> names, IEnumerable<SPTerm> fileTypeTerms)
        {
            _confluenceClient = confluenceClient;
            _names = names;
            _fileTypeTerms = fileTypeTerms;
        }

        public SFile Convert(ListItem listItem)
        {
            try
            {
                var userGuideFileName = "User guide";

                var fileName = listItem.Get<String>(SharepointFields.FileLeaf);

                var fileType = listItem.Get<TaxonomyFieldValue>(SharepointFields.ManualFileType).ToSPTerm(_fileTypeTerms);

                var title = listItem.Get<String>(SharepointFields.Title);

                var devices = listItem.Get<TaxonomyFieldValueCollection>(SharepointFields.Devices).ToSPTermList(_names);

                var created = listItem.Get<DateTime>(SharepointFields.Created);

                var fileUrl = listItem.Get<FieldUrlValue>(SharepointFields.UrlUpperCase).ToFileUrlStr(fileName);

                Debug.WriteLine($"Start loading manual '{fileUrl}'");

                var sFile = new SFile
                {
                    Id = listItem.Id,
                    Name = fileName,
                    Title = title,
                    Devices = devices,
                    File_type = fileType,
                    Created = created,
                    Url = fileUrl
                };

                if (fileType.OwnNameFromPath == userGuideFileName)
                {
                    var contentId = _confluenceClient.GetContentIdFromUrl(fileUrl);

                    if (contentId.HasValue)
                    {
                        var content = _confluenceClient.GetUserManual(contentId.Value, listItem.Id);

                        PushUserGuideToCache(content);

                        var url = UrlBuilder.GetRelativeDeviceGuideUrl(PrepareUrlName(content)).ToString();

                        sFile.Url = url;
                    }
                    else
                    {
                        Debug.WriteLine($"End loading manual '{fileUrl}'");

                        return null;
                    }

                }

                Debug.WriteLine($"End loading manual '{fileUrl}'");

                return sFile;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private static String PrepareUrlName(UserManualModel content)
        {
            return content.Title.Replace("/", "");
        }

        private void PushUserGuideToCache(UserManualModel model)
        {
            try
            {
                HttpRuntime.Cache.Insert(model.Title, model, new TimerCacheDependency());

                var fileName = $"{model.Title.Replace("/", "")}_{CultureInfo.CurrentCulture.IetfLanguageTag}.dat";

                var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content\\Data\\UserGuides", fileName);
                
                Stream streamWrite = null;

                var dir = Path.GetDirectoryName(filePath);

                if (!Directory.Exists(dir))
                {
                    var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content\\Data\\UserGuides", fileName);

                    Directory.CreateDirectory(folderPath);
                }

                try
                {
                    streamWrite = File.Create(filePath);
                    var binaryWrite = new BinaryFormatter();
                    binaryWrite.Serialize(streamWrite, model);
                    streamWrite.Close();
                }
                catch (DirectoryNotFoundException)
                {
                    var folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content\\Data\\UserGuides", fileName);

                    var result = Directory.CreateDirectory(folderPath);
                    if (result.Exists)
                        PushUserGuideToCache(model);
                }
                catch (Exception ex)
                {
                    if (streamWrite != null) streamWrite.Close();
                    //toDo log exception to log file
                }
            }
            catch (Exception ex)
            {
                //toDo log exception to log file
            }
        }
    }
}