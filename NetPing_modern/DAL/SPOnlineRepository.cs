using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using NetpingHelpers;
using NetPing.Global.Config;
using NetPing.Models;
using NetPing.PriceGeneration;
using NetPing.PriceGeneration.YandexMarker;
using NetPing.Tools;
using NetPing_modern.DAL;
using NetPing_modern.PriceGeneration;
using NetPing_modern.Resources;
using NetPing_modern.Resources.Views.Catalog;
using NetPing_modern.Services.Confluence;
//using TidyManaged;
using Category = NetPing.PriceGeneration.YandexMarker.Category;
using File = System.IO.File;
using System.Data;
using Microsoft.VisualBasic.FileIO;

namespace NetPing.DAL
{
    internal class SPOnlineRepository : IRepository
    {
        private const string TermsCategoriesCacheName = "TermsCategories";
        private readonly IConfluenceClient _confluenceClient;

        public SPOnlineRepository(IConfluenceClient confluenceClient)
        {
            _confluenceClient = confluenceClient;
        }

        #region Properties

        public IEnumerable<SPTerm> TermsLabels { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsLabels")); } }
        private IEnumerable<SPTerm> TermsLabels_Read() { return GetTermsFromSP("Labels"); }

        public IEnumerable<SPTerm> TermsCategories
        {
            get
            {
                return (IEnumerable<SPTerm>)(PullFromCache(TermsCategoriesCacheName));
            }
        }

        private IEnumerable<SPTerm> TermsCategories_Read()
        {
            return GetTermsFromSP("Posts categories");
        }


        public IEnumerable<SPTerm> TermsDeviceParameters { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsDeviceParameters")); } }
        private IEnumerable<SPTerm> TermsDeviceParameters_Read() { return GetTermsFromSP("Device parameters"); }

        public IEnumerable<SPTerm> TermsFileTypes { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsFileTypes")); } }
        private IEnumerable<SPTerm> TermsFileTypes_Read() { return GetTermsFromSP("Documents types"); }

        public IEnumerable<SPTerm> TermsDestinations { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsDestinations")); } }
        private IEnumerable<SPTerm> TermsDestinations_Read() { return GetTermsFromSP("Destinations"); }

        public IEnumerable<SPTerm> Terms { get { return (IEnumerable<SPTerm>)(PullFromCache("Terms")); } }
        private IEnumerable<SPTerm> Terms_Read() { return GetTermsFromSP("Names"); }

        public IEnumerable<SPTerm> TermsFirmwares { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsFirmwares")); } }
        private IEnumerable<SPTerm> TermsFirmwares_Read() { return GetTermsFromSP("Firmware versions"); }
        /*
        /*
                public IEnumerable<SPTerm> TermsSiteTexts { get { return (IEnumerable<SPTerm>)(PullFromCache("TermsSiteTexts")); } }
                private IEnumerable<SPTerm> TermsSiteTexts_Read() { return GetTermsFromSP("Site texts"); }
        */

        public IEnumerable<SiteText> SiteTexts { get { return (IEnumerable<SiteText>)(PullFromCache("SiteTexts")); } }
        private IEnumerable<SiteText> SiteTexts_Read()
        {
            var result = new List<SiteText>();

            foreach (var item in (ListItemCollection)ReadSPList("Web_texts", Camls.Caml_SiteTexts))
            {
                var link = item["Body_link"] as FieldUrlValue;
                int? contentId = null;
                string url = null;
                if (link != null)
                {
                    url = link.Url;
                    contentId = _confluenceClient.GetContentIdFromUrl(url);
                }
                string content = string.Empty;
                string title = string.Empty;
                if (contentId.HasValue)
                {
                    Task<string> contentTask = _confluenceClient.GetContenAsync(contentId.Value);
                    content = contentTask.Result;
                    contentTask = _confluenceClient.GetContentTitleAsync(contentId.Value);
                    title = contentTask.Result;
                }

                result.Add(new SiteText
                {
                    Tag = item["Title"] as string
                   ,
                    Text = content.ReplaceInternalLinks()
                });
            }
            if (result.Count == 0) throw new Exception("No one SiteText was readed!");
            return result;
        }


        public IEnumerable<DeviceParameter> DevicesParameters { get { return (IEnumerable<DeviceParameter>)(PullFromCache("DevicesParameters")); } }
        private IEnumerable<DeviceParameter> DevicesParameters_Read(IEnumerable<SPTerm> termsDeviceParameters, IEnumerable<SPTerm> terms)
        {
            var result = new List<DeviceParameter>();

            foreach (var item in (ListItemCollection)ReadSPList("Device_parameters", ""))
            {
                result.Add(new DeviceParameter
                {
                    Id = item.Id
                   ,
                    Name = (item["Parameter"] as TaxonomyFieldValue).ToSPTerm(termsDeviceParameters)
                   ,
                    Device = (item["Device"] as TaxonomyFieldValue).ToSPTerm(terms)
                   ,
                    Value = (Helpers.IsCultureEng) ? item["ENG_value"] as string : item["Title"] as string
                });
            }
            if (result.Count == 0) throw new Exception("No one deviceparameter was readed!");
            return result;
        }




        public IEnumerable<Device> Devices
        {
            get
            {
                var terms = new List<SPTerm>(Terms);
                var devices = (IEnumerable<Device>)(PullFromCache("Devices"));
                var index = BuildIndex(devices, terms);
                return devices.OrderBy(d => index[d]);
            }
        }

        private Dictionary<Device, int> BuildIndex(IEnumerable<Device> list, List<SPTerm> terms)
        {
            var result = new Dictionary<Device, int>();
            foreach (var device in list)
            {
                var i = FindIndex(terms, s => s.Id == device.Name.Id);
                result.Add(device, i > -1 ? i : int.MaxValue);
            }
            return result;
        }

        private static int FindIndex<T>(IEnumerable<T> items, Func<T, bool> predicate)
        {
            int index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                    return index;
                index++;
            }
            return -1;
        }

        private readonly Regex _contentIdRegex = new Regex(@"pageId=(?<id>\d+)");
        private readonly Regex _spaceTitleRegex = new Regex(@"\/display\/(?<spaceKey>[\w \.\-\+%]+)\/(?<title>[\w \.\-\+%]+)?");

        private void LoadFromConfluence(Device device, Expression<Func<Device, string>> expression, string url)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<Device>());
            var propertyInfo = typeof(Device).GetProperty(meta.PropertyName);


            var id = _confluenceClient.GetContentIdFromUrl(url);
            if (id == null)
            {
                propertyInfo.SetValue(device, string.Empty);
                return;
            }

            var contentTask = _confluenceClient.GetContenAsync((int)id);
            string content = contentTask.Result;
            if (!string.IsNullOrWhiteSpace(content))
            {
                string propertyValue = ReplaceConfluenceImages(StylishHeaders3(CleanSpanStyles(CleanFonts((content)))));
                propertyInfo.SetValue(device, propertyValue);
            }
            /*
                        var mc = _contentIdRegex.Matches(url);
                        if (mc.Count > 0)
                        {
                            Match m = mc[0];
                            if (m.Success)
                            {
                                Group group = m.Groups["id"];
                                int id = int.Parse(group.Value);

                                var contentTask = _confluenceClient.GetContenAsync(id);
                                string content = contentTask.Result;
                                if (!string.IsNullOrWhiteSpace(content))
                                {
                                    string propertyValue = ReplaceConfluenceImages(StylishHeaders3(CleanSpanStyles(CleanFonts((content)))));

                                    var utf8 = Encoding.UTF8.GetBytes(propertyValue);
                                    var win1251 = Encoding.GetEncoding(1251).GetString(utf8);
                                    var doc = Document.FromString(win1251);
                                    doc.DocType = DocTypeMode.Auto;
                                    doc.OutputBodyOnly = AutoBool.Yes;
                                    doc.OutputXhtml = true;
                                    doc.ShowWarnings = false;
                                    doc.IndentBlockElements = AutoBool.Yes;
                                    doc.RemoveEndTags = true;

                                    doc.CleanAndRepair();
                                    propertyValue = doc.Save();

                                    propertyInfo.SetValue(device, propertyValue);
                                }
                            }
                        }
                        else
                        {
                            mc = _spaceTitleRegex.Matches(url);
                            if (mc.Count > 0)
                            {
                                Match m = mc[0];
                                if (m.Success)
                                {
                                    Group spaceKeyGroup = m.Groups["spaceKey"];
                                    string spaceKey = spaceKeyGroup.Value;

                                    Group titleGroup = m.Groups["title"];
                                    string title = titleGroup.Value;

                                    var contentTask = _confluenceClient.GetContentBySpaceAndTitle(spaceKey, title);
                                    int contentId = contentTask.Result;
                                    if (contentId > 0)
                                    {
                                        var contentTask2 = _confluenceClient.GetContenAsync(contentId);
                                        string content = contentTask2.Result;
                                        if (!string.IsNullOrWhiteSpace(content))
                                        {
                                            string propertyValue = ReplaceConfluenceImages(StylishHeaders3(CleanSpanStyles(CleanFonts((content)))));
                                            propertyInfo.SetValue(device, propertyValue);
                                        }
                                    }
                                }
                            }
                        }
            */





            if (propertyInfo.GetValue(device) == null)
            {
                propertyInfo.SetValue(device, string.Empty);
            }
        }

        private IEnumerable<Device> Devices_Read(IEnumerable<Post> allPosts, IEnumerable<SFile> allFiles, IEnumerable<DevicePhoto> allDevicePhotos, IEnumerable<DeviceParameter> allDevicesParameters, IEnumerable<SPTerm> terms, IEnumerable<SPTerm> termsDestinations, IEnumerable<SPTerm> termsLabels)
        {
            var devices = new List<Device>();

            string stock_csv = HttpContext.Current.Server.MapPath("~/Pub/Data/netping_ru_stock.csv");
            var dataTable = new Dictionary<string, string>();
            var _eviceStockUpdate = new DateTime();

            if (File.Exists(stock_csv))
            {
                dataTable = GetDataTableFromCSVFile(stock_csv);
                _eviceStockUpdate = DateTime.Parse(dataTable[""]);
            }

            foreach (var item in (ListItemCollection)ReadSPList("Devices", Camls.Caml_Device_keys))
            {
                //var _guidid1S = item["1C_ref"];
                var _guidid = item["_x0031_C_ref"] as string;
                var _stock = _guidid != null && dataTable.ContainsKey(_guidid.ToString()) ? dataTable[_guidid.ToString()] : "-1";

                var device = new Device
                {
                    Id = item.Id
                            ,
                    OldKey = item["Title"] as string
                            ,
                    Name = (item["Name"] as TaxonomyFieldValue).ToSPTerm(terms)
                            ,
                    Destination = (item["Destination"] as TaxonomyFieldValueCollection).ToSPTermList(termsDestinations)
                            ,
                    Connected_devices = (item["Connected_devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                            ,
                    Price = item["Price"] as double?
                            ,
                    Label = (item["Label"] as TaxonomyFieldValue).ToSPTerm(termsLabels)
                            ,
                    Created = (DateTime)item["Created"]
                            ,
                    Url = item["Url"] as string
                            ,
                    DeviceStock = int.Parse(_stock)
                            ,
                    DeviceStockUpdate = DateTime.Now
                };



                var urlField = item["Short_descr"] as FieldUrlValue;
                if (urlField != null)
                {
                    LoadFromConfluence(device, d => d.Short_description, urlField.Url);
                }

                urlField = item["Long_descr"] as FieldUrlValue;
                if (urlField != null)
                {
                    LoadFromConfluence(device, d => d.Long_description, urlField.Url);
                }

                devices.Add(device);
            }


            foreach (var dev in devices)
            {

                //                Debug.WriteLine(dev.Name.Name);
                // Collect Posts and Sfiles corresponded to device

                dev.Posts = allPosts.Where(pst => dev.Name.IsIncludeAnyFromOthers(pst.Devices) || dev.Name.IsUnderAnyOthers(pst.Devices)).ToList();
                dev.SFiles = allFiles.Where(fl => dev.Name.IsIncludeAnyFromOthers(fl.Devices) || dev.Name.IsUnderAnyOthers(fl.Devices)).ToList();


                // collect device parameters 
                dev.DeviceParameters = allDevicesParameters.Where(par => par.Device == dev.Name).ToList();

                // Get device photos
                dev.DevicePhotos = allDevicePhotos.Where(p => p.Dev_name == dev.Name).ToList();

            }

            if (devices.Count == 0) throw new Exception("No one devices was readed!");
            return devices;
        }

        /// <summary>
        /// read csv file to DataTable
        /// </summary>
        /// <param name="csv_file_path"></param>
        /// <returns></returns>
        private Dictionary<string, string> GetDataTableFromCSVFile(string csv_file_path)
        {
            var csvData = new Dictionary<string, string>();
            try
            {
                using (TextFieldParser csvReader = new TextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiters(new string[] { "," });
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    //read column names  
                    csvReader.ReadLine();

                    while (!csvReader.EndOfData)
                    {
                        string[] fields = csvReader.ReadFields();
                        csvData.Add(fields[0], fields[1]);
                    }
                }
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);  
            }
            return csvData;
        }

        private String CleanSpanStyles(String str)
        {
            for (int tagstart = str.IndexOf("<span"); tagstart != -1; tagstart = str.IndexOf("<span", tagstart + 1))
            {
                int tagend = str.IndexOf('>', tagstart);
                var tag = str.Substring(tagstart, tagend - tagstart + 1);
                if (tag.Contains("style"))
                {
                    str = str.Remove(
                        tag.IndexOf("style") + tagstart,
                        tag.LastIndexOf('"') - tag.IndexOf("style") + 1);
                }
            }
            return str;
        }

        private String CleanFonts(String str)
        {
            for (int tagstart = str.IndexOf("<font"); tagstart != -1; tagstart = str.IndexOf("<font", tagstart + 1))
            {
                int tagend = str.IndexOf('>', tagstart);
                var tag = str.Substring(tagstart, tagend - tagstart + 1);
                if (tag.Contains("color"))
                {
                    str = str.Remove(
                        tag.IndexOf("color") + tagstart,
                        tag.LastIndexOf('"') - tag.IndexOf("color") + 1);
                }
            }
            return str;
        }

        private String StylishHeaders3(String str)
        {
            //str = str.Replace("<h3", "<h3 class=\"shutter collapsed\"><span class=dashed>");
            //str = str.Replace("</h3>", "</span></h3>");
            return str;
        }

        private readonly Regex ConfluenceImageTagRegex = new Regex(@"\<img [^\>]+\>", RegexOptions.IgnoreCase);

        private string ReplaceConfluenceImages(string str)
        {
            return ConfluenceImageTagRegex.Replace(str, new MatchEvaluator(ConfluenceImage));
        }

        private readonly Regex ConfluenceImgSrcRegex = new Regex(@"\ssrc=""/wiki(?<src>[^\""]+)""");
        private readonly Regex ConfluenceDataBaseUrlRegex = new Regex(@"\sdata-base-url=""(?<src>[^\""]+)""");

        string ConfluenceImage(Match match)
        {
            var s = match.ToString();
            if (s.Contains("confluence-embedded-image"))
            {
                var src = ConfluenceImgSrcRegex.Match(s);
                var baseUrl = ConfluenceDataBaseUrlRegex.Match(s);
                if (src.Success && baseUrl.Success)
                {
                    return ConfluenceImgSrcRegex.Replace(s, string.Format(" src=\"{0}\"", baseUrl.Groups["src"].Value + src.Groups["src"].Value));
                }
            }
            return s;
        }

        public IEnumerable<DevicePhoto> DevicePhotos { get { return (IEnumerable<DevicePhoto>)(PullFromCache("DevicePhotos")); } }
        private IEnumerable<DevicePhoto> DevicePhotos_Read(IEnumerable<SPTerm> terms)
        {
            var result = new List<DevicePhoto>();

            foreach (var item in (ListItemCollection)ReadSPList("Device_photos", Camls.Caml_DevicePhotos))
            {
                string pictureUrl = (item["FileLeafRef"] as string);
                if (!string.IsNullOrEmpty(pictureUrl))
                {
                    pictureUrl = pictureUrl.Replace(" ", string.Empty);
                }
                result.Add(new DevicePhoto
                {
                    Name = item["FileLeafRef"] as string
                   ,
                    Dev_name = ((item["Device"] == null) ? null : item["Device"] as TaxonomyFieldValue).ToSPTerm(terms)
                   ,
                    Url = "http://www.netping.ru/Pub/Photos/" + pictureUrl
                   ,
                    IsBig = pictureUrl.Contains("big") ? true : false
                   ,
                    IsCover = Convert.ToBoolean(item["Cover"])
                });
            }
            if (result.Count == 0) throw new Exception("No one DevicePhoto was readed!");
            return result;
        }

        public IEnumerable<PubFiles> PubFiles { get { return (IEnumerable<PubFiles>)(PullFromCache("PubFiles")); } }
        private IEnumerable<PubFiles> PubFiles_Read(IEnumerable<SPTerm> termsFileTypes)
        {
            var result = new List<PubFiles>();

            foreach (var item in (ListItemCollection)ReadSPList("Photos_to_pub", Camls.Caml_Photos_to_pub))
            {
                result.Add(new PubFiles
                {
                    Name = item["FileLeafRef"] as string
                   ,
                    File_type = (item["File_type"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes)
                   ,
                    Url = "http://netping.ru/Pub/Pub/" + (item["FileLeafRef"] as string)
                   ,
                    Url_link = (item["Url"] as FieldUrlValue).Url
                });
            }
            //if (result.Count == 0) throw new Exception("No one PubFiles was readed!");
            return result;
        }

        public IEnumerable<SFile> SFiles { get { return (IEnumerable<SFile>)(PullFromCache("SFiles")); } }
        private IEnumerable<SFile> SFiles_Read(IEnumerable<SPTerm> termsFileTypes, IEnumerable<SPTerm> terms)
        {
            var result = new List<SFile>();

            foreach (var item in (ListItemCollection)ReadSPList("Pub files", Camls.Caml_Pub_files))
            {

                result.Add(new SFile
                {
                    Id = item.Id
                   ,
                    Name = item["FileLeafRef"] as string
                   ,
                    Title = item["Title"] as string
                   ,
                    Devices = (item["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                   ,
                    File_type = (item["File_x0020_type0"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes)
                   ,
                    Created = (DateTime)item["Created"]
                   ,
                    Url = (item["Public_url"] as FieldUrlValue).ToFileUrlStr(item["FileLeafRef"] as string)

                });
            }
            if (result.Count == 0) throw new Exception("No one SFile was readed!");


            var _context = new ClientContext("https://netpingeastcoltd.sharepoint.com/dev/");
            _context.Credentials = new SharePointOnlineCredentials(_config.SPSettings.Login, _config.SPSettings.Password.ToSecureString());
            _context.ExecuteQuery();
            var list = _context.Web.Lists.GetByTitle("Firmwares");
            CamlQuery camlquery = new CamlQuery();

            camlquery.ViewXml = NetPing_modern.Resources.Camls.Caml_Firmwares;
            var items = list.GetItems(camlquery);
            _context.Load(list);
            _context.Load(items);
            _context.ExecuteQuery();


            foreach (var item in items)
            {

                Folder item_folder = _context.Web.GetFolderByServerRelativeUrl(item["FileDirRef"].ToString());
                Folder item_folder_parent = item_folder.ParentFolder;
                var item_folder_parent_items = item_folder_parent.ListItemAllFields;
                _context.Load(item_folder);
                _context.Load(item_folder_parent);
                _context.Load(item_folder_parent_items);
                _context.ExecuteQuery();

                var file_type = termsFileTypes.FirstOrDefault(t => t.Id == new Guid("4dadfd09-f883-4f42-9178-ded2fe88016b"));
                if ((item["DocType"] as TaxonomyFieldValue).TermGuid == "e3de2072-1eb2-4b6d-a7e2-3319bf89836d") file_type = termsFileTypes.FirstOrDefault(t => t.Id == new Guid("e3de2072-1eb2-4b6d-a7e2-3319bf89836d"));

                result.Add(new SFile
                {
                    Id = item.Id
                   ,
                    Name = item["FileLeafRef"] as string
                   ,
                    Title = item["Title"] as string
                   ,
                    Devices = (item_folder_parent_items["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                   ,
                    File_type = file_type
                   ,
                    Created = (DateTime)item["Created"]
                   ,
                    Url = "http://netping.ru/Pub/Firmwares/" + (item["FileLeafRef"] as string)

                });
            }
            if (result.Count == 0) throw new Exception("No one SFile was readed!");


            return result;
        }

        private readonly Regex tagRegex = new Regex("\\[.*\\]");

        public IEnumerable<Post> Posts { get { return (IEnumerable<Post>)(PullFromCache("Posts")); } }
        private IEnumerable<Post> Posts_Read(IEnumerable<SPTerm> terms, IEnumerable<SPTerm> categoryTerms)
        {
            var result = new List<Post>();

            foreach (var item in (ListItemCollection)ReadSPList("Blog_posts", Camls.Caml_Posts))
            {
                var link = item["Body_link"] as FieldUrlValue;
                int? contentId = null;
                string url = null;
                if (link != null)
                {
                    url = link.Url;
                    contentId = _confluenceClient.GetContentIdFromUrl(url);
                }
                string content = string.Empty;
                string title = string.Empty;
                if (contentId.HasValue)
                {
                    Task<string> contentTask = _confluenceClient.GetContenAsync(contentId.Value);
                    content = contentTask.Result;
                    contentTask = _confluenceClient.GetContentTitleAsync(contentId.Value);
                    title = contentTask.Result;
                }

                if (!string.IsNullOrWhiteSpace(title))
                {
                    title = tagRegex.Replace(title, "");
                }



                result.Add(new Post
                {
                    Id = (item["Old_id"] == null) ? 0 : int.Parse(item["Old_id"].ToString())
                            ,
                    Title = title
                            ,
                    Devices = (item["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                            ,
                    Body = content.ReplaceInternalLinks()
                            ,
                    Category = (item["Category"] as TaxonomyFieldValue).ToSPTerm(categoryTerms)
                            ,
                    Created = (DateTime)item["Pub_date"]
                             ,
                    Url_name = "/Blog/" + (item["Body_link"] as FieldUrlValue).Description.Replace(".", "x2E").Trim(' '),
                    IsTop = (bool)item["TOP"]
                });
            }
            if (result.Count == 0) throw new Exception("No one post was readed!");
            return result;
        }

        #endregion


        public string UpdateAll()
        {
            try
            {
                var termsFileTypes = TermsFileTypes_Read(); Debug.WriteLine("TermsFileTypes_Read OK");
                var terms = Terms_Read(); Debug.WriteLine("Terms_Read OK");

                var termsLabels = TermsLabels_Read(); Debug.WriteLine("TermsLabels_Read OK");
                var termsCategories = TermsCategories_Read(); Debug.WriteLine("TermsCategories_Read OK");
                var termsDeviceParameters = TermsDeviceParameters_Read(); Debug.WriteLine("TermsDeviceParameters_Read OK");

                var termsDestinations = TermsDestinations_Read(); Debug.WriteLine("TermsDestinations_Read OK");
                //                var termsSiteTexts = TermsSiteTexts_Read();
                //var termFirmwares = TermsFirmwares_Read();
                var siteTexts = SiteTexts_Read();
                var devicesParameters = DevicesParameters_Read(termsDeviceParameters, terms);
                var devicePhotos = DevicePhotos_Read(terms); Debug.WriteLine("DevicePhotos_Read OK");
                var pubFiles = PubFiles_Read(termsFileTypes);
                var sFiles = SFiles_Read(termsFileTypes, terms); Debug.WriteLine("SFiles_Read OK");
                var posts = Posts_Read(terms, termsCategories); Debug.WriteLine("Posts_Read OK");
                var devices = Devices_Read(posts, sFiles, devicePhotos, devicesParameters, terms, termsDestinations, termsLabels); Debug.WriteLine("Devices_Read OK");

                PushToCache("SiteTexts", siteTexts);
                PushToCache("TermsLabels", termsLabels);
                PushToCache(TermsCategoriesCacheName, termsCategories);
                PushToCache("TermsDeviceParameters", termsDeviceParameters);
                PushToCache("TermsFileTypes", termsFileTypes);
                PushToCache("TermsDestinations", termsDestinations);
                PushToCache("Terms", terms);
                //PushToCache("TermsFirmwares", terms);
                //                PushToCache("TermsSiteTexts", termsSiteTexts);
                PushToCache("DevicesParameters", devicesParameters);
                PushToCache("DevicePhotos", devicePhotos);
                PushToCache("PubFiles", pubFiles);
                PushToCache("SFiles", sFiles);
                PushToCache("Posts", posts);
                PushToCache("Devices", devices);

                Debug.WriteLine("PushToCache OK");

                if (Helpers.IsCultureRus)
                {
                    //   GeneratePriceList();
                    GenerateYml();
                }
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

            return "OK!";

        }

        public string UpdateAllAsync(string name)
        {
            IEnumerable<SPTerm> terms = null, termsDeviceParameters = null, termsFileTypes = null, termsCategories = null, termsDestinations = null, termsLabels = null;
            IEnumerable<Post> posts = null;
            IEnumerable<SFile> sFiles = null;
            IEnumerable<DevicePhoto> devicePhotos = null;
            IEnumerable<DeviceParameter> devicesParameters = null;
            IEnumerable<PubFiles> pubFiles = null;
            IEnumerable<Device> devices = null;
            IEnumerable<SiteText> siteTexts = null;

            switch (name)
            {
                case "TermsFileTypes":
                    termsFileTypes = TermsFileTypes_Read();
                    HttpRuntime.Cache.Insert("TermsFileTypes", termsFileTypes);
                    //PushToCache("TermsFileTypes", termsFileTypes);
                    break;
                case "Terms":
                    terms = Terms_Read();
                    HttpRuntime.Cache.Insert("Terms", terms);
                    //PushToCache("Terms", terms);
                    break;
                case "SiteTexts":
                    siteTexts = SiteTexts_Read();
                    HttpRuntime.Cache.Insert("SiteTexts", siteTexts);
                    //PushToCache("SiteTexts", siteTexts);
                    break;
                case "TermsLabels":
                    termsLabels = TermsLabels_Read();
                    HttpRuntime.Cache.Insert("TermsLabels", termsLabels);
                    //PushToCache("TermsLabels", termsLabels);
                    break;
                case "TermsDeviceParameters":
                    termsDeviceParameters = TermsDeviceParameters_Read();
                    HttpRuntime.Cache.Insert("TermsDeviceParameters", termsDeviceParameters);
                    //PushToCache("TermsDeviceParameters", termsDeviceParameters);
                    break;
                case "TermsCategories":
                    termsCategories = TermsCategories_Read();
                    HttpRuntime.Cache.Insert("TermsCategories", termsCategories);
                    //PushToCache(TermsCategoriesCacheName, termsCategories);
                    break;
                case "TermsDestinations":
                    termsDestinations = TermsDestinations_Read();
                    HttpRuntime.Cache.Insert("TermsDestinations", termsDestinations);
                    //PushToCache("TermsDestinations", termsDestinations);
                    break;
                case "DevicesParameters":
                    terms = Terms;
                    termsDeviceParameters = TermsDeviceParameters;
                    devicesParameters = DevicesParameters_Read(termsDeviceParameters, terms);
                    HttpRuntime.Cache.Insert("DevicesParameters", devicesParameters);
                    //PushToCache("DevicesParameters", devicesParameters);
                    break;
                case "DevicePhotos":
                    terms = Terms;
                    devicePhotos = DevicePhotos_Read(terms);
                    HttpRuntime.Cache.Insert("DevicePhotos", devicePhotos);
                    //PushToCache("DevicePhotos", devicePhotos);
                    break;
                case "PubFiles":
                    termsFileTypes = TermsFileTypes;
                    pubFiles = PubFiles_Read(termsFileTypes);
                    HttpRuntime.Cache.Insert("PubFiles", pubFiles);
                    //PushToCache("PubFiles", pubFiles);
                    break;
                case "SFiles":
                    terms = Terms;
                    termsFileTypes = TermsFileTypes;
                    sFiles = SFiles_Read(termsFileTypes, terms);
                    HttpRuntime.Cache.Insert("SFiles", sFiles);
                    //PushToCache("SFiles", sFiles);
                    break;
                case "Posts":
                    terms = Terms;
                    termsCategories = TermsCategories;
                    posts = Posts_Read(terms, termsCategories);
                    HttpRuntime.Cache.Insert("Posts", posts);
                    //PushToCache("Posts", posts);
                    break;
                case "Devices":
                    terms = Terms;
                    termsCategories = TermsCategories;
                    posts = Posts;
                    termsFileTypes = TermsFileTypes;
                    sFiles = SFiles;
                    devicePhotos = DevicePhotos;
                    termsDeviceParameters = TermsDeviceParameters;
                    devicesParameters = DevicesParameters;
                    termsDestinations = TermsDestinations;
                    termsLabels = TermsLabels;
                    devices = Devices_Read(posts, sFiles, devicePhotos, devicesParameters, terms, termsDestinations, termsLabels);
                    HttpRuntime.Cache.Insert("Devices", devices);
                    PushToCache("Devices", devices);
                    break;
                case "GenerateYml":
                    if (Helpers.IsCultureRus)
                    {
                        //   GeneratePriceList();
                        GenerateYml();
                    }
                    break;
                case "PushAll":
                    PushToCache("TermsFileTypes", TermsFileTypes);
                    PushToCache("Terms", Terms);
                    PushToCache("SiteTexts", SiteTexts);
                    PushToCache("TermsLabels", TermsLabels);
                    PushToCache("TermsDeviceParameters", TermsDeviceParameters);
                    PushToCache(TermsCategoriesCacheName, TermsCategories);
                    PushToCache("TermsDestinations", TermsDestinations);
                    PushToCache("DevicesParameters", DevicesParameters);
                    PushToCache("DevicePhotos", DevicePhotos);
                    PushToCache("PubFiles", PubFiles);
                    PushToCache("SFiles", SFiles);
                    PushToCache("Posts", Posts);
                    //PushToCache("Devices", Devices);
                    break;
                default:
                    return "404";
            }
            return "OK";
        }


        private void GenerateYml()
        {
            var catalog = new YmlCatalog
            {
                Date = DateTime.Now
            };
            var shop = new Shop();
            catalog.Shop = shop;


            const string netpingRu = "Netping.ru";
            shop.Name = netpingRu;
            shop.Company = netpingRu;
            shop.Url = "http://www.netping.ru";
            shop.Currencies.Add(new Currency
            {
                Id = "RUR",
                Rate = 1,
                Plus = 0
            });

            var tree = new DevicesTree(Devices);
            foreach (DeviceTreeNode categoryNode in tree.Nodes)
            {
                shop.Categories.Add(new Category
                {
                    Id = categoryNode.Id,
                    Name = categoryNode.Name,
                    ParentId = categoryNode.Parent == null ? (int?)null : categoryNode.Parent.Id
                });

                foreach (DeviceTreeNode childCategoryNode in categoryNode.Nodes)
                {
                    AddOffers(childCategoryNode, shop, categoryNode);
                }
            }
            shop.LocalDeliveryCost = 350;

            YmlGenerator.Generate(catalog, HttpContext.Current.Server.MapPath("/Content/Data/netping.xml"));
        }

        private static void AddOffers(DeviceTreeNode offerNode, Shop shop, DeviceTreeNode childCategoryNode)
        {
            if (!(string.IsNullOrEmpty(offerNode.Device.Label.OwnNameFromPath) ||
                  offerNode.Device.Label.OwnNameFromPath.Equals("New", StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            string shortDescription = offerNode.Device.Short_description;
            string descr = string.Empty;
            if (!string.IsNullOrWhiteSpace(shortDescription))
            {
                var htmlDoc = new HtmlDocument();

                htmlDoc.LoadHtml(shortDescription);
                var ulNodes = htmlDoc.DocumentNode.SelectNodes("//ul");
                if (ulNodes != null)
                {
                    foreach (var ulNode in ulNodes)
                    {
                        ulNode.Remove();
                    }
                }
                descr = htmlDoc.DocumentNode.InnerText.Replace("&#160;", " ");
            }

            shop.Offers.Add(new Offer
            {
                Id = offerNode.Id,
                Url = GetDeviceUrl(offerNode.Device),
                Price = (int)(offerNode.Device.Price.HasValue ? offerNode.Device.Price.Value : 0),
                CategoryId = childCategoryNode.Id,
                Picture = offerNode.Device.GetCoverPhoto(true).Url,
                TypePrefix = "",
                /*childCategoryNode.Name,*/
                VendorCode = offerNode.Name,
                Model = offerNode.Name,
                Description = descr
            });

        }

        public IEnumerable<Device> GetDevices(string id, string groupId)
        {
            if (string.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            if (string.IsNullOrEmpty(groupId))
                throw new ArgumentNullException("groupId");

            //var group = Devices.FirstOrDefault(d => d.Key == groupId + "#");
            var group = Devices.FirstOrDefault(d => d.Url == groupId);
            var devices = Devices.Where(d => d.Name.IsUnderOther(group.Name) && !d.Name.IsGroup());
            return devices;
        }

        private void GeneratePriceList()
        {
            using (var priceList = new PriceList())
            {
                var monitoring = new NetPing_modern.PriceGeneration.Category(Index.Sec_monitoring);

                monitoring.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.MonitoringId, CategoryId.MonitoringSection.DevicesId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_sensors, CategoryId.MonitoringId, CategoryId.MonitoringSection.SensorsId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.MonitoringId, CategoryId.MonitoringSection.AccessoriesId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.MonitoringId, CategoryId.MonitoringSection.SolutionsId));
                priceList.Categories.Add(monitoring);

                var power = new NetPing_modern.PriceGeneration.Category(Index.Sec_power);
                power.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.PowerId, CategoryId.PowerSection.DevicesId));
                power.Sections.Add(new Section(this, Index.Sec_sub_sensors, CategoryId.PowerId, CategoryId.PowerSection.SensorsId));
                power.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.PowerId, CategoryId.PowerSection.AccessoriesId));
                power.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.PowerId, CategoryId.PowerSection.SolutionsId));
                priceList.Categories.Add(power);


                var switches = new NetPing_modern.PriceGeneration.Category(Index.Sec_switch);
                switches.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.SwitchesId, CategoryId.SwitchesSection.DevicesId));
                switches.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.SwitchesId, CategoryId.SwitchesSection.AccessoriesId));
                switches.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.SwitchesId, CategoryId.SwitchesSection.SolutionsId));
                priceList.Categories.Add(switches);

                var priceRepl = new PriceListReplacementsTree();
                var categoriesRepl = new CategoriesReplacementsTree();
                var sectionsRepl = new SectionsReplacemenetsTree();
                var productsRepl = new ProductsReplacementsTree();

                sectionsRepl.Add("%StartProducts%*%EndProducts%", productsRepl);
                categoriesRepl.Add("%StartSections%*%EndSections%", sectionsRepl);
                priceRepl.Add("%StartCategories%*%EndCategories%", categoriesRepl);

                var generator = new PriceListGenerator(priceRepl);
                generator.Generate(priceList, new FileInfo(HttpContext.Current.Server.MapPath("Content/Price/price_template.docx")),
                    HttpContext.Current.Server.MapPath("Content/Data/Price.pdf"));
            }
        }

        internal static string GetDeviceUrl(Device device)
        {
            /* var url =
                 LinkBuilder.BuildUrlFromExpression<Product_itemController>(
                     new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()),
                     RouteTable.Routes, c => c.Index(device.Key));
             Uri uri = HttpContext.Current.Request.Url;
             url = string.Format("{0}://{1}{2}{3}", uri.Scheme, uri.Authority, HttpRuntime.AppDomainAppVirtualPath, url);*/
            return "http://www.netping.ru/products/" + device.Url;
        }


        private object PullFromCache(string cache_name)
        {
            object obj = HttpRuntime.Cache.Get(cache_name);
            if (obj != null) return obj;

            // Check file cache
            Stream streamRead = null;
            string file_name = HttpContext.Current.Server.MapPath("~/Content/Data/" + cache_name + "_" + CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
            try
            {
                streamRead = File.OpenRead(file_name);
                BinaryFormatter binaryRead = new BinaryFormatter();
                obj = (object)binaryRead.Deserialize(streamRead);
                streamRead.Close();
            }
            catch (Exception ex)
            {
                if (streamRead != null) streamRead.Close();
                UpdateAll();
                return HttpRuntime.Cache.Get(cache_name);
            }
            HttpRuntime.Cache.Insert(cache_name, obj, new TimerCacheDependency());

            return obj;
        }

        private void PushToCache(string cache_name, object obj)
        {
            HttpRuntime.Cache.Insert(cache_name, obj, new TimerCacheDependency());

            string file_name = HttpContext.Current.Server.MapPath("~/Content/Data/" + cache_name + "_" + CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
            Stream streamWrite = null;
            try
            {
                streamWrite = File.Create(file_name);
                BinaryFormatter binaryWrite = new BinaryFormatter();
                binaryWrite.Serialize(streamWrite, obj);
                streamWrite.Close();
            }
            catch (Exception ex)
            {
                if (streamWrite != null) streamWrite.Close();
                //toDo log exception to log file
            }
        }

        private IEnumerable<SPTerm> GetTermsFromSP(string setname)
        {
            var lcid = CultureInfo.CurrentCulture.LCID;

            var terms = new List<SPTerm>();
            var sortOrders = new SortOrdersCollection<Guid>();

            var session = TaxonomySession.GetTaxonomySession(context);
            var termSets = session.GetTermSetsByName(setname, 1033);
            context.Load(session);
            context.Load(termSets);
            context.ExecuteQuery();

            var allTerms = termSets[0].GetAllTerms();
            context.Load(allTerms);
            context.ExecuteQuery();

            foreach (var term in allTerms)
            {
                string name = term.Name;

                if (lcid != 1033)   // If lcid label not avaliable or lcid==1033 keep default label
                {
                    var lang_label = term.GetAllLabels(lcid);
                    context.Load(lang_label);
                    context.ExecuteQuery();

                    if (lang_label.Count != 0) name = lang_label[0].Value;
                }

                terms.Add(new SPTerm
                {
                    Id = term.Id
                           ,
                    Name = name
                           ,
                    Path = term.PathOfTerm
                           ,
                    Properties = term.LocalCustomProperties
                });
                if (!string.IsNullOrEmpty(term.CustomSortOrder))
                {
                    sortOrders.AddSortOrder(term.CustomSortOrder);
                }

            }
            var customSortOrder = sortOrders.GetSortOrders();

            terms.Sort(new SPTermComparerByCustomSortOrder(customSortOrder));

            if (terms.Count == 0) throw new Exception("No terms was readed!");

            return terms;
        }

        public TreeNode<Device> DevicesTree(Device root, IEnumerable<Device> devices)
        {
            var tree = new TreeNode<Device>(root);
            BuildTree(tree, devices);

            return tree;
        }

        private void BuildTree(TreeNode<Device> dev, IEnumerable<Device> list)
        {
            var childrens = list.Where(d => d.Name.Level == dev.Value.Name.Level + 1 && d.Name.Path.Contains(dev.Value.Name.Path));
            foreach (var child in childrens)
            {
                BuildTree(dev.AddChild(child), list);
            }
        }

        //private IEnumerable<HTMLInjection> _htmlInjections;
        public IEnumerable<HTMLInjection> HtmlInjections
        {
            get
            {
                var _htmlInjections = HttpRuntime.Cache.Get("HtmlInjection");

                if (_htmlInjections == null)
                {
                    _htmlInjections = ReadHTMLInjection();
                    HttpRuntime.Cache.Insert("HtmlInjection", _htmlInjections, new TimerCacheDependency());
                }

                return _htmlInjections as IEnumerable<HTMLInjection>;
            }
        }

        private IEnumerable<HTMLInjection> ReadHTMLInjection()
        {
            //var _context = new ClientContext("https://netpingeastcoltd.sharepoint.com/dev/");
            //_context.Credentials = new SharePointOnlineCredentials(_config.SPSettings.Login, _config.SPSettings.Password.ToSecureString());
            //_context.ExecuteQuery();
            //var list = _context.Web.Lists.GetByTitle("HTML_injection");
            //CamlQuery camlquery = new CamlQuery();

            //camlquery.ViewXml = NetPing_modern.Resources.Camls.Caml_HTMLInjection;
            //var items = list.GetItems(camlquery);
            //_context.Load(list);
            //_context.Load(items);
            //_context.ExecuteQuery();

            var items = ReadSPList("HTML_injection", NetPing_modern.Resources.Camls.Caml_HTMLInjection);

            var list = new List<HTMLInjection>();
            foreach (var item in items)
            {
                list.Add(new HTMLInjection
                {
                    HTML = item["HTML"].ToString(),
                    Page = item["Page"].ToString(),
                    Section = item["Section"].ToString(),
                    Title = item["Title"].ToString(),
                });
            }

            return list;

            //foreach(var item in items)
            //{
            //    var title = item["Title"];
            //    var page = item["Page"];
            //    var section = item["Section"];
            //    var html = item["HTML"];


            //}
        }

        public string GetHtmlInjectionForPage(string name, string page, string section = "Head")
        {
            return HtmlInjections.FirstOrDefault(x => x.Title == name && x.Page == page && x.Section == section).HTML;
        }

        #region SharePoint Context


        private ListItemCollection ReadSPList(string list_name, string caml_query)
        {
            var list = context.Web.Lists.GetByTitle(list_name);
            CamlQuery camlquery = new CamlQuery();
            camlquery.ViewXml = caml_query;
            var items = list.GetItems(camlquery);
            context.Load(list);
            context.Load(items);
            context.ExecuteQuery();
            return items;
        }

        private static IConfig _config
        {
            get
            {
                return new Config();

                //return DependencyResolver.Current.GetService<NetPing_modern.Global.Config.IConfig>();
            }
        }

        private ClientContext _context;
        protected ClientContext context
        {
            get
            {

                if (_context != null)
                    return _context;

                _context = new ClientContext(_config.SPSettings.SiteUrl);

                _context.RequestTimeout = _config.SPSettings.RequestTimeout;
                _context.Credentials = new SharePointOnlineCredentials(_config.SPSettings.Login, _config.SPSettings.Password.ToSecureString());
                _context.ExecuteQuery();

                return _context;
            }
        }
        #endregion

        #region Disposing
        public void Dispose()
        {
            Dispose(true);
        }

        private bool disposed = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (_context != null)
                    {
                        _context.Dispose();
                        _context = null;
                    }
                }
            }
            this.disposed = true;
        }
        #endregion
    }
}