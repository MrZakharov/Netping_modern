using System;
using System.Collections.Generic;
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
        private readonly ConfluenceClient _confluenceClient;
        private readonly IEnumerable<SPTerm> _names;
        private readonly IEnumerable<SPTerm> _fileTypeTerms;

        public DeviceManualFileConverter(ConfluenceClient confluenceClient, IEnumerable<SPTerm> names, IEnumerable<SPTerm> fileTypeTerms)
        {
            _confluenceClient = confluenceClient;
            _names = names;
            _fileTypeTerms = fileTypeTerms;
        }

        public SFile Convert(ListItem listItem)
        {
            var fileType = listItem["File_x0020_type0"] as TaxonomyFieldValue;

            if (fileType.ToSPTerm(_fileTypeTerms).OwnNameFromPath == "User guide")
            {
                var fileUrl = (listItem["URL"] as FieldUrlValue).ToFileUrlStr(listItem["FileLeafRef"].ToString());

                var contentId = _confluenceClient.GetContentIdFromUrl(fileUrl);

                if (contentId.HasValue)
                {
                    var content = _confluenceClient.GetUserManual(contentId.Value, listItem.Id);

                    PushUserGuideToCache(content);
                    var url = "/UserGuide/" + content.Title.Replace("/", "");

                    var sFile = new SFile
                    {
                        Id = listItem.Id,
                        Name = listItem["FileLeafRef"] as String,
                        Title = listItem["Title"] as String,
                        Devices = (listItem["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(_names),
                        File_type = fileType.ToSPTerm(_fileTypeTerms),
                        Created = (DateTime) listItem["Created"],
                        Url = url
                    };

                    return sFile;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                var sFile = new SFile
                {
                    Id = listItem.Id,
                    Name = listItem["FileLeafRef"].ToString(),
                    Title = listItem["Title"].ToString(),
                    Devices = (listItem["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(_names),
                    File_type = fileType.ToSPTerm(_fileTypeTerms),
                    Created = (DateTime) listItem["Created"],
                    Url = (listItem["URL"] as FieldUrlValue).ToFileUrlStr(listItem["FileLeafRef"].ToString())
                };

                return sFile;
            }
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