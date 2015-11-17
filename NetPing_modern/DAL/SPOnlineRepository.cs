using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using HtmlAgilityPack;
using Microsoft.SharePoint.Client;
using Microsoft.SharePoint.Client.Taxonomy;
using Microsoft.VisualBasic.FileIO;
using NetpingHelpers;
using NetPing.Global.Config;
using NetPing.Models;
using NetPing.PriceGeneration;
using NetPing.PriceGeneration.YandexMarker;
using NetPing.Tools;
using NetPing_modern.DAL;
using NetPing_modern.DAL.Model;
using NetPing_modern.PriceGeneration;
using NetPing_modern.Resources;
using NetPing_modern.Resources.Views.Catalog;
using NetPing_modern.Services.Confluence;
using Category = NetPing.PriceGeneration.YandexMarker.Category;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class SPOnlineRepository : IRepository
    {
        public SPOnlineRepository(IConfluenceClient confluenceClient)
        {
            _confluenceClient = confluenceClient;
        }

        #region :: Public Methods ::

        public String UpdateAll()
        {
            try
            {
                var termsFileTypes = TermsFileTypes_Read();
                Debug.WriteLine("TermsFileTypes_Read OK");
                var terms = Terms_Read();
                Debug.WriteLine("Terms_Read OK");

                var termsLabels = TermsLabels_Read();
                Debug.WriteLine("TermsLabels_Read OK");
                var termsCategories = TermsCategories_Read();
                Debug.WriteLine("TermsCategories_Read OK");
                var termsDeviceParameters = TermsDeviceParameters_Read();
                Debug.WriteLine("TermsDeviceParameters_Read OK");

                var termsDestinations = TermsDestinations_Read();
                Debug.WriteLine("TermsDestinations_Read OK");
                //                var termsSiteTexts = TermsSiteTexts_Read();
                //var termFirmwares = TermsFirmwares_Read();
                var siteTexts = SiteTexts_Read();
                var devicesParameters = DevicesParameters_Read(termsDeviceParameters, terms);
                var devicePhotos = DevicePhotos_Read(terms);
                Debug.WriteLine("DevicePhotos_Read OK");
                var pubFiles = PubFiles_Read(termsFileTypes);
                var sFiles = SFiles_Read(termsFileTypes, terms);
                Debug.WriteLine("SFiles_Read OK");
                var posts = Posts_Read(terms, termsCategories);
                Debug.WriteLine("Posts_Read OK");
                var devices = Devices_Read(posts, sFiles, devicePhotos, devicesParameters, terms, termsDestinations,
                    termsLabels);
                Debug.WriteLine("Devices_Read OK");

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

        public String UpdateAllAsync(String name)
        {
            IEnumerable<SPTerm> terms = null,
                termsDeviceParameters = null,
                termsFileTypes = null,
                termsCategories = null,
                termsDestinations = null,
                termsLabels = null;
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
                    devices = Devices_Read(posts, sFiles, devicePhotos, devicesParameters, terms, termsDestinations,
                        termsLabels);
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
                case "OnlyDevices":

                    break;
                default:
                    return "404";
            }
            return "OK";
        }

        public IEnumerable<Device> GetDevices(String id, String groupId)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException("id");

            if (String.IsNullOrEmpty(groupId))
                throw new ArgumentNullException("groupId");

            //var group = Devices.FirstOrDefault(d => d.Key == groupId + "#");
            var group = Devices.FirstOrDefault(d => d.Url == groupId);
            var devices = Devices.Where(d => d.Name.IsUnderOther(group.Name) && !d.Name.IsGroup());
            return devices;
        }

        public TreeNode<Device> DevicesTree(Device root, IEnumerable<Device> devices)
        {
            var tree = new TreeNode<Device>(root);
            BuildTree(tree, devices);

            return tree;
        }

        public void PushUserGuideToCache(UserManualModel model)
        {
            try
            {
                HttpRuntime.Cache.Insert(model.Title, model, new TimerCacheDependency());

                var file_name =
                    HttpContext.Current.Server.MapPath("~/Content/Data/UserGuides/" + model.Title.Replace("/", "") + "_" +
                                                       CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
                Stream streamWrite = null;
                try
                {
                    streamWrite = File.Create(file_name);
                    var binaryWrite = new BinaryFormatter();
                    binaryWrite.Serialize(streamWrite, model);
                    streamWrite.Close();
                }
                catch (DirectoryNotFoundException)
                {
                    var result =
                        Directory.CreateDirectory(HttpContext.Current.Server.MapPath("~/Content/Data/UserGuides/"));
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

        public String GetHtmlInjectionForPage(String name, String page, String section = "Head")
        {
            return HtmlInjections.FirstOrDefault(x => x.Title == name && x.Page == page && x.Section == section).HTML;
        }
        public static String GetDeviceUrl(Device device)
        {
            /* var url =
                 LinkBuilder.BuildUrlFromExpression<Product_itemController>(
                     new RequestContext(new HttpContextWrapper(HttpContext.Current), new RouteData()),
                     RouteTable.Routes, c => c.Index(device.Key));
             Uri uri = HttpContext.Current.Request.Url;
             url = string.Format("{0}://{1}{2}{3}", uri.Scheme, uri.Authority, HttpRuntime.AppDomainAppVirtualPath, url);*/
            return "http://www.netping.ru/products/" + device.Url;
        }

        #endregion

        #region :: Public Properties ::

        public IEnumerable<Post> Posts
        {
            get { return (IEnumerable<Post>)(PullFromCache("Posts")); }
        }

        public IEnumerable<SFile> SFiles
        {
            get { return (IEnumerable<SFile>)(PullFromCache("SFiles")); }
        }

        public IEnumerable<PubFiles> PubFiles
        {
            get { return (IEnumerable<PubFiles>)(PullFromCache("PubFiles")); }
        }

        public IEnumerable<DevicePhoto> DevicePhotos
        {
            get { return (IEnumerable<DevicePhoto>)(PullFromCache("DevicePhotos")); }
        }

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

        public IEnumerable<SPTerm> TermsLabels
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("TermsLabels")); }
        }

        public IEnumerable<SPTerm> TermsCategories
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache(TermsCategoriesCacheName)); }
        }

        public IEnumerable<SPTerm> TermsDeviceParameters
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("TermsDeviceParameters")); }
        }

        public IEnumerable<SPTerm> TermsFileTypes
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("TermsFileTypes")); }
        }

        public IEnumerable<SPTerm> TermsDestinations
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("TermsDestinations")); }
        }

        public IEnumerable<SPTerm> Terms
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("Terms")); }
        }

        public IEnumerable<SiteText> SiteTexts
        {
            get { return (IEnumerable<SiteText>)(PullFromCache("SiteTexts")); }
        }

        public IEnumerable<DeviceParameter> DevicesParameters
        {
            get { return (IEnumerable<DeviceParameter>)(PullFromCache("DevicesParameters")); }
        }

        public IEnumerable<SPTerm> TermsFirmwares
        {
            get { return (IEnumerable<SPTerm>)(PullFromCache("TermsFirmwares")); }
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

        #endregion

        #region :: Private Methods ::

        private ListItemCollection ReadSPList(String list_name, String caml_query)
        {
            var list = context.Web.Lists.GetByTitle(list_name);
            var camlquery = new CamlQuery();
            camlquery.ViewXml = caml_query;
            var items = list.GetItems(camlquery);
            context.Load(list);
            context.Load(items);
            context.ExecuteQuery();
            return items;
        }

        private String ConfluenceImage(Match match)
        {
            var s = match.ToString();
            if (s.Contains("confluence-embedded-image"))
            {
                var src = ConfluenceImgSrcRegex.Match(s);
                var baseUrl = ConfluenceDataBaseUrlRegex.Match(s);
                if (src.Success && baseUrl.Success)
                {
                    return ConfluenceImgSrcRegex.Replace(s,
                        String.Format(" src=\"{0}\"", baseUrl.Groups["src"].Value + src.Groups["src"].Value));
                }
            }
            return s;
        }

        private IEnumerable<DevicePhoto> DevicePhotos_Read(IEnumerable<SPTerm> terms)
        {
            var result = new List<DevicePhoto>();

            foreach (var item in ReadSPList("Device_photos", Camls.Caml_DevicePhotos))
            {
                var pictureUrl = (item["FileLeafRef"] as String);
                if (!String.IsNullOrEmpty(pictureUrl))
                {
                    pictureUrl = pictureUrl.Replace(" ", String.Empty);
                }
                result.Add(new DevicePhoto
                {
                    Name = item["FileLeafRef"] as String
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

        private IEnumerable<PubFiles> PubFiles_Read(IEnumerable<SPTerm> termsFileTypes)
        {
            var result = new List<PubFiles>();

            foreach (var item in ReadSPList("Photos_to_pub", Camls.Caml_Photos_to_pub))
            {
                result.Add(new PubFiles
                {
                    Name = item["FileLeafRef"] as String
                    ,
                    File_type = (item["File_type"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes)
                    ,
                    Url = "http://netping.ru/Pub/Pub/" + (item["FileLeafRef"] as String)
                    ,
                    Url_link = (item["Url"] as FieldUrlValue).Url
                });
            }
            //if (result.Count == 0) throw new Exception("No one PubFiles was readed!");
            return result;
        }

        private IEnumerable<SFile> SFiles_Read(IEnumerable<SPTerm> termsFileTypes, IEnumerable<SPTerm> terms)
        {
            var result = new List<SFile>();
            var confluenceClient = new ConfluenceClient(new Config());

            foreach (var item in ReadSPList("Device documentation", Camls.Caml_DevDoc))
            {
                if ((item["File_x0020_type0"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes).OwnNameFromPath ==
                    "User guide")
                {
                    //TODO: Save to file content by FileLeafRef param
                    var fileUrl = (item["URL"] as FieldUrlValue).ToFileUrlStr(item["FileLeafRef"] as String);

                    var contentId = confluenceClient.GetContentIdFromUrl(fileUrl);
                    if (contentId.HasValue)
                    {
                        var content = confluenceClient.GetUserManual(contentId.Value, item.Id);
                        PushUserGuideToCache(content);
                        //TODO: Save url to file as Url param
                        //var url = new UrlHelper().Action("UserGuide", "Products", new { id = contentId.Value });
                        try
                        {
                            var url = "/UserGuide/" + content.Title.Replace("/", "");
                            result.Add(new SFile
                            {
                                Id = item.Id
                                ,
                                Name = item["FileLeafRef"] as String
                                ,
                                Title = item["Title"] as String
                                ,
                                Devices = (item["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                                ,
                                File_type = (item["File_x0020_type0"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes)
                                ,
                                Created = (DateTime)item["Created"]
                                ,
                                Url = url
                            });
                        }
                        catch (Exception ex)
                        {
                            //toDo log exception to log file
                        }
                    }
                }
                else
                {
                    result.Add(new SFile
                    {
                        Id = item.Id
                        ,
                        Name = item["FileLeafRef"] as String
                        ,
                        Title = item["Title"] as String
                        ,
                        Devices = (item["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                        ,
                        File_type = (item["File_x0020_type0"] as TaxonomyFieldValue).ToSPTerm(termsFileTypes)
                        ,
                        Created = (DateTime)item["Created"]
                        ,
                        Url = (item["URL"] as FieldUrlValue).ToFileUrlStr(item["FileLeafRef"] as String)
                    });
                }
            }
            if (result.Count == 0) throw new Exception("No one SFile was readed!");


            var _context = new ClientContext("https://netpingeastcoltd.sharepoint.com/dev/");
            _context.Credentials = new SharePointOnlineCredentials(_config.SPSettings.Login,
                _config.SPSettings.Password.ToSecureString());
            _context.ExecuteQuery();
            var list = _context.Web.Lists.GetByTitle("Firmwares");
            var camlquery = new CamlQuery();

            camlquery.ViewXml = Camls.Caml_Firmwares;
            var items = list.GetItems(camlquery);
            _context.Load(list);
            _context.Load(items);
            _context.ExecuteQuery();


            foreach (var item in items)
            {
                var item_folder = _context.Web.GetFolderByServerRelativeUrl(item["FileDirRef"].ToString());
                var item_folder_parent = item_folder.ParentFolder;
                var item_folder_parent_items = item_folder_parent.ListItemAllFields;
                _context.Load(item_folder);
                _context.Load(item_folder_parent);
                _context.Load(item_folder_parent_items);
                _context.ExecuteQuery();

                var file_type =
                    termsFileTypes.FirstOrDefault(t => t.Id == new Guid("4dadfd09-f883-4f42-9178-ded2fe88016b"));
                if ((item["DocType"] as TaxonomyFieldValue).TermGuid == "e3de2072-1eb2-4b6d-a7e2-3319bf89836d")
                    file_type =
                        termsFileTypes.FirstOrDefault(t => t.Id == new Guid("e3de2072-1eb2-4b6d-a7e2-3319bf89836d"));

                result.Add(new SFile
                {
                    Id = item.Id
                    ,
                    Name = item["FileLeafRef"] as String
                    ,
                    Title = item["Title"] as String
                    ,
                    Devices = (item_folder_parent_items["Devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                    ,
                    File_type = file_type
                    ,
                    Created = (DateTime)item["Created"]
                    ,
                    Url = "http://netping.ru/Pub/Firmwares/" + (item["FileLeafRef"] as String)
                });
            }
            if (result.Count == 0) throw new Exception("No one SFile was readed!");


            return result;
        }

        private IEnumerable<Post> Posts_Read(IEnumerable<SPTerm> terms, IEnumerable<SPTerm> categoryTerms)
        {
            var result = new List<Post>();

            foreach (var item in ReadSPList("Blog_posts", Camls.Caml_Posts))
            {
                var link = item["Body_link"] as FieldUrlValue;
                Int32? contentId = null;
                String url = null;
                if (link != null)
                {
                    url = link.Url;
                    contentId = _confluenceClient.GetContentIdFromUrl(url);
                }
                var content = String.Empty;
                var title = String.Empty;
                Dictionary<String, String> metaHtml = null;
                if (contentId.HasValue)
                {
                    var contentTask = _confluenceClient.GetContenAsync(contentId.Value);
                    content = contentTask.Result;

                    metaHtml = GetPageProperties(content);
                    if (metaHtml != null)
                        content = RemovePagePropertiesInContent(content);

                    contentTask = _confluenceClient.GetContentTitleAsync(contentId.Value);
                    title = contentTask.Result;
                }

                if (!String.IsNullOrWhiteSpace(title))
                {
                    title = tagRegex.Replace(title, "");
                }


                result.Add(new Post
                {
                    Id = (item["Old_id"] == null) ? 0 : Int32.Parse(item["Old_id"].ToString())
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
                    IsTop = (Boolean)item["TOP"],
                    MetaHtml = metaHtml
                });
            }
            if (result.Count == 0) throw new Exception("No one post was readed!");
            return result;
        }

        /// <summary>
        ///     Находим блок PageProperties на странице
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private Dictionary<String, String> GetPageProperties(String content)
        {
            var html = new HtmlDocument();
            html.LoadHtml(content);
            try
            {
                var table = GetPagePropertiesContent(html).Descendants("table").FirstOrDefault();
                if (table != null)
                {
                    var result = new Dictionary<String, String>();
                    var trNodes = table.ChildNodes[0].ChildNodes.Where(x => x.Name == "tr");
                    foreach (var tr in trNodes)
                    {
                        var tdNodes = tr.ChildNodes.Where(x => x.Name == "td").ToArray();
                        if (tdNodes.Count() == 2)
                        {
                            var key = tdNodes[0].InnerText;
                            if (!result.ContainsKey(key))
                                result.Add(tdNodes[0].InnerText, tdNodes[1].InnerText);
                        }
                    }
                    return result;
                }
                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        ///     Удаляем из страницы блок PageProperties (блок с мета тегами)
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private String RemovePagePropertiesInContent(String content)
        {
            var html = new HtmlDocument();
            html.LoadHtml(content);
            var table = GetPagePropertiesContent(html);
            html.DocumentNode.SelectSingleNode(table.XPath).Remove();
            return html.DocumentNode.InnerHtml;
        }

        /// <summary>
        ///     Находим на странице блок PageProperties (блок с мета тегами)
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static HtmlNode GetPagePropertiesContent(HtmlDocument html)
        {
            return html.DocumentNode.Descendants("div").Where(x => x.Attributes.Contains("data-macro-name")
                                                                   &&
                                                                   x.Attributes["data-macro-name"].Value.Contains(
                                                                       "details")).FirstOrDefault();
        }

        private void LoadFromConfluence(Device device, Expression<Func<Device, String>> expression, String url)
        {
            var meta = ModelMetadata.FromLambdaExpression(expression, new ViewDataDictionary<Device>());
            var propertyInfo = typeof(Device).GetProperty(meta.PropertyName);


            var id = _confluenceClient.GetContentIdFromUrl(url);
            if (id == null)
            {
                propertyInfo.SetValue(device, String.Empty);
                return;
            }

            var contentTask = _confluenceClient.GetContenAsync((Int32)id);
            var content = contentTask.Result;
            if (!String.IsNullOrWhiteSpace(content))
            {
                var propertyValue = ReplaceConfluenceImages(StylishHeaders3(CleanSpanStyles(CleanFonts((content)))));
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
                propertyInfo.SetValue(device, String.Empty);
            }
        }

        private IEnumerable<Device> Devices_Read(IEnumerable<Post> allPosts, IEnumerable<SFile> allFiles,
            IEnumerable<DevicePhoto> allDevicePhotos, IEnumerable<DeviceParameter> allDevicesParameters,
            IEnumerable<SPTerm> terms, IEnumerable<SPTerm> termsDestinations, IEnumerable<SPTerm> termsLabels)
        {
            var devices = new List<Device>();

            var stock_csv = HttpContext.Current.Server.MapPath("~/Pub/Data/netping_ru_stock.csv");
            var dataTable = new Dictionary<String, String>();
            var _eviceStockUpdate = new DateTime();

            if (File.Exists(stock_csv))
            {
                dataTable = GetDataTableFromCSVFile(stock_csv);
                _eviceStockUpdate = DateTime.Parse(dataTable[""]);
            }

            foreach (var item in ReadSPList("Devices", Camls.Caml_Device_keys))
            {
                //var _guidid1S = item["1C_ref"];
                var _guidid = item["_x0031_C_ref"] as String;
                var _stock = _guidid != null && dataTable.ContainsKey(_guidid) ? dataTable[_guidid] : "-1";

                var device = new Device
                {
                    Id = item.Id
                    ,
                    OldKey = item["Title"] as String
                    ,
                    Name = (item["Name"] as TaxonomyFieldValue).ToSPTerm(terms)
                    ,
                    Destination = (item["Destination"] as TaxonomyFieldValueCollection).ToSPTermList(termsDestinations)
                    ,
                    Connected_devices = (item["Connected_devices"] as TaxonomyFieldValueCollection).ToSPTermList(terms)
                    ,
                    Price = item["Price"] as Double?
                    ,
                    Label = (item["Label"] as TaxonomyFieldValue).ToSPTerm(termsLabels)
                    ,
                    Created = (DateTime)item["Created"]
                    ,
                    Url = item["Url"] as String
                    ,
                    DeviceStockUpdate = _eviceStockUpdate
                    ,
                    DeviceStock = Int16.Parse(_stock)
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

                dev.Posts =
                    allPosts.Where(
                        pst => dev.Name.IsIncludeAnyFromOthers(pst.Devices) || dev.Name.IsUnderAnyOthers(pst.Devices))
                        .ToList();
                dev.SFiles =
                    allFiles.Where(
                        fl => dev.Name.IsIncludeAnyFromOthers(fl.Devices) || dev.Name.IsUnderAnyOthers(fl.Devices))
                        .ToList();


                // collect device parameters 
                dev.DeviceParameters = allDevicesParameters.Where(par => par.Device == dev.Name).ToList();

                // Get device photos
                dev.DevicePhotos = allDevicePhotos.Where(p => p.Dev_name.Id == dev.Name.Id).ToList();
            }

            if (devices.Count == 0) throw new Exception("No one devices was readed!");
            return devices;
        }

        /// <summary>
        ///     read csv file to DataTable
        /// </summary>
        /// <param name="csv_file_path"></param>
        /// <returns></returns>
        private Dictionary<String, String> GetDataTableFromCSVFile(String csv_file_path)
        {
            var csvData = new Dictionary<String, String>();
            try
            {
                using (var csvReader = new TextFieldParser(csv_file_path))
                {
                    csvReader.SetDelimiters(",");
                    csvReader.HasFieldsEnclosedInQuotes = true;

                    //read column names  
                    csvReader.ReadLine();

                    while (!csvReader.EndOfData)
                    {
                        var fields = csvReader.ReadFields();
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
            for (var tagstart = str.IndexOf("<span"); tagstart != -1; tagstart = str.IndexOf("<span", tagstart + 1))
            {
                var tagend = str.IndexOf('>', tagstart);
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
            for (var tagstart = str.IndexOf("<font"); tagstart != -1; tagstart = str.IndexOf("<font", tagstart + 1))
            {
                var tagend = str.IndexOf('>', tagstart);
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

        private String ReplaceConfluenceImages(String str)
        {
            return ConfluenceImageTagRegex.Replace(str, ConfluenceImage);
        }

        private IEnumerable<SPTerm> TermsDestinations_Read()
        {
            return GetTermsFromSP("Destinations");
        }

        private IEnumerable<SPTerm> Terms_Read()
        {
            return GetTermsFromSP("Names");
        }

        private IEnumerable<SPTerm> TermsFirmwares_Read()
        {
            return GetTermsFromSP("Firmware versions");
        }

        private IEnumerable<DeviceParameter> DevicesParameters_Read(IEnumerable<SPTerm> termsDeviceParameters,
            IEnumerable<SPTerm> terms)
        {
            var result = new List<DeviceParameter>();

            foreach (var item in ReadSPList("Device_parameters", ""))
            {
                result.Add(new DeviceParameter
                {
                    Id = item.Id
                    ,
                    Name = (item["Parameter"] as TaxonomyFieldValue).ToSPTerm(termsDeviceParameters)
                    ,
                    Device = (item["Device"] as TaxonomyFieldValue).ToSPTerm(terms)
                    ,
                    Value = (Helpers.IsCultureEng) ? item["ENG_value"] as String : item["Title"] as String
                });
            }
            if (result.Count == 0) throw new Exception("No one deviceparameter was readed!");
            return result;
        }

        private Dictionary<Device, Int32> BuildIndex(IEnumerable<Device> list, List<SPTerm> terms)
        {
            var result = new Dictionary<Device, Int32>();
            foreach (var device in list)
            {
                var i = FindIndex(terms, s => s.Id == device.Name.Id);
                result.Add(device, i > -1 ? i : Int32.MaxValue);
            }
            return result;
        }

        private static Int32 FindIndex<T>(IEnumerable<T> items, Func<T, Boolean> predicate)
        {
            var index = 0;
            foreach (var item in items)
            {
                if (predicate(item))
                    return index;
                index++;
            }
            return -1;
        }

        private IEnumerable<SiteText> SiteTexts_Read()
        {
            var result = new List<SiteText>();

            foreach (var item in ReadSPList("Web_texts", Camls.Caml_SiteTexts))
            {
                var link = item["Body_link"] as FieldUrlValue;
                Int32? contentId = null;
                String url = null;
                if (link != null)
                {
                    url = link.Url;
                    contentId = _confluenceClient.GetContentIdFromUrl(url);
                }
                var content = String.Empty;
                var title = String.Empty;
                if (contentId.HasValue)
                {
                    var contentTask = _confluenceClient.GetContenAsync(contentId.Value);
                    content = contentTask.Result;
                    contentTask = _confluenceClient.GetContentTitleAsync(contentId.Value);
                    title = contentTask.Result;
                }

                result.Add(new SiteText
                {
                    Tag = item["Title"] as String
                    ,
                    Text = content.ReplaceInternalLinks()
                });
            }
            if (result.Count == 0) throw new Exception("No one SiteText was readed!");
            return result;
        }

        private void GenerateYml()
        {
            var catalog = new YmlCatalog
            {
                Date = DateTime.Now
            };
            var shop = new Shop();
            catalog.Shop = shop;


            const String netpingRu = "Netping.ru";
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
            foreach (var categoryNode in tree.Nodes)
            {
                shop.Categories.Add(new Category
                {
                    Id = categoryNode.Id,
                    Name = categoryNode.Name,
                    ParentId = categoryNode.Parent == null ? (Int32?)null : categoryNode.Parent.Id
                });

                foreach (var childCategoryNode in categoryNode.Nodes)
                {
                    AddOffers(childCategoryNode, shop, categoryNode);
                }
            }
            shop.LocalDeliveryCost = 350;

            YmlGenerator.Generate(catalog, HttpContext.Current.Server.MapPath("/Content/Data/netping.xml"));
        }

        private static void AddOffers(DeviceTreeNode offerNode, Shop shop, DeviceTreeNode childCategoryNode)
        {
            if (!(String.IsNullOrEmpty(offerNode.Device.Label.OwnNameFromPath) ||
                  offerNode.Device.Label.OwnNameFromPath.Equals("New", StringComparison.CurrentCultureIgnoreCase)))
            {
                return;
            }

            var shortDescription = offerNode.Device.Short_description;
            var descr = String.Empty;
            if (!String.IsNullOrWhiteSpace(shortDescription))
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
                Price = (Int32)(offerNode.Device.Price.HasValue ? offerNode.Device.Price.Value : 0),
                CategoryId = childCategoryNode.Id,
                Picture = offerNode.Device.GetCoverPhoto(true).Url,
                TypePrefix = "",
                /*childCategoryNode.Name,*/
                VendorCode = offerNode.Name,
                Model = offerNode.Name,
                Description = descr
            });
        }

        private void GeneratePriceList()
        {
            using (var priceList = new PriceList())
            {
                var monitoring = new NetPing_modern.PriceGeneration.Category(Index.Sec_monitoring);

                monitoring.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.MonitoringId,
                    CategoryId.MonitoringSection.DevicesId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_sensors, CategoryId.MonitoringId,
                    CategoryId.MonitoringSection.SensorsId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.MonitoringId,
                    CategoryId.MonitoringSection.AccessoriesId));
                monitoring.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.MonitoringId,
                    CategoryId.MonitoringSection.SolutionsId));
                priceList.Categories.Add(monitoring);

                var power = new NetPing_modern.PriceGeneration.Category(Index.Sec_power);
                power.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.PowerId,
                    CategoryId.PowerSection.DevicesId));
                power.Sections.Add(new Section(this, Index.Sec_sub_sensors, CategoryId.PowerId,
                    CategoryId.PowerSection.SensorsId));
                power.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.PowerId,
                    CategoryId.PowerSection.AccessoriesId));
                power.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.PowerId,
                    CategoryId.PowerSection.SolutionsId));
                priceList.Categories.Add(power);


                var switches = new NetPing_modern.PriceGeneration.Category(Index.Sec_switch);
                switches.Sections.Add(new Section(this, Index.Sec_sub_devices, CategoryId.SwitchesId,
                    CategoryId.SwitchesSection.DevicesId));
                switches.Sections.Add(new Section(this, Index.Sec_sub_access, CategoryId.SwitchesId,
                    CategoryId.SwitchesSection.AccessoriesId));
                switches.Sections.Add(new Section(this, Index.Sec_sub_solutions, CategoryId.SwitchesId,
                    CategoryId.SwitchesSection.SolutionsId));
                priceList.Categories.Add(switches);

                var priceRepl = new PriceListReplacementsTree();
                var categoriesRepl = new CategoriesReplacementsTree();
                var sectionsRepl = new SectionsReplacemenetsTree();
                var productsRepl = new ProductsReplacementsTree();

                sectionsRepl.Add("%StartProducts%*%EndProducts%", productsRepl);
                categoriesRepl.Add("%StartSections%*%EndSections%", sectionsRepl);
                priceRepl.Add("%StartCategories%*%EndCategories%", categoriesRepl);

                var generator = new PriceListGenerator(priceRepl);
                generator.Generate(priceList,
                    new FileInfo(HttpContext.Current.Server.MapPath("Content/Price/price_template.docx")),
                    HttpContext.Current.Server.MapPath("Content/Data/Price.pdf"));
            }
        }

        private Object PullFromCache(String cache_name)
        {
            var obj = HttpRuntime.Cache.Get(cache_name);
            if (obj != null) return obj;

            // Check file cache
            Stream streamRead = null;
            var file_name =
                HttpContext.Current.Server.MapPath("~/Content/Data/" + cache_name + "_" +
                                                   CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
            try
            {
                streamRead = File.OpenRead(file_name);
                var binaryRead = new BinaryFormatter();
                obj = binaryRead.Deserialize(streamRead);
                streamRead.Close();
            }
            catch (Exception ex)
            {
                if (streamRead != null) streamRead.Close();
                // UpdateAll();
                return HttpRuntime.Cache.Get(cache_name);
            }
            HttpRuntime.Cache.Insert(cache_name, obj, new TimerCacheDependency());

            return obj;
        }

        private void PushToCache(String cache_name, Object obj)
        {
            HttpRuntime.Cache.Insert(cache_name, obj, new TimerCacheDependency());

            var file_name =
                HttpContext.Current.Server.MapPath("~/Content/Data/" + cache_name + "_" +
                                                   CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
            Stream streamWrite = null;
            try
            {
                streamWrite = File.Create(file_name);
                var binaryWrite = new BinaryFormatter();
                binaryWrite.Serialize(streamWrite, obj);
                streamWrite.Close();
            }
            catch (Exception ex)
            {
                if (streamWrite != null) streamWrite.Close();
                //toDo log exception to log file
            }
        }

        private IEnumerable<SPTerm> GetTermsFromSP(String setname)
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
                var name = term.Name;

                if (lcid != 1033) // If lcid label not avaliable or lcid==1033 keep default label
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
                if (!String.IsNullOrEmpty(term.CustomSortOrder))
                {
                    sortOrders.AddSortOrder(term.CustomSortOrder);
                }
            }
            var customSortOrder = sortOrders.GetSortOrders();

            terms.Sort(new SPTermComparerByCustomSortOrder(customSortOrder));

            if (terms.Count == 0) throw new Exception("No terms was readed!");

            return terms;
        }

        private void BuildTree(TreeNode<Device> dev, IEnumerable<Device> list)
        {
            var childrens =
                list.Where(d => d.Name.Level == dev.Value.Name.Level + 1 && d.Name.Path.Contains(dev.Value.Name.Path));
            foreach (var child in childrens)
            {
                BuildTree(dev.AddChild(child), list);
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

            var items = ReadSPList("HTML_injection", Camls.Caml_HTMLInjection);

            var list = new List<HTMLInjection>();
            foreach (var item in items)
            {
                list.Add(new HTMLInjection
                {
                    HTML = item["HTML"].ToString(),
                    Page = item["Page"].ToString(),
                    Section = item["Section"].ToString(),
                    Title = item["Title"].ToString()
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

        private IEnumerable<SPTerm> TermsLabels_Read()
        {
            return GetTermsFromSP("Labels");
        }

        private IEnumerable<SPTerm> TermsCategories_Read()
        {
            return GetTermsFromSP("Posts categories");
        }

        private IEnumerable<SPTerm> TermsDeviceParameters_Read()
        {
            return GetTermsFromSP("Device parameters");
        }

        private IEnumerable<SPTerm> TermsFileTypes_Read()
        {
            return GetTermsFromSP("Documents types");
        }

        #endregion

        #region :: Private Properties ::

        private static IConfig _config
        {
            get
            {
                return new Config();

                //return DependencyResolver.Current.GetService<NetPing_modern.Global.Config.IConfig>();
            }
        }

        private ClientContext context
        {
            get
            {
                if (_context != null)
                    return _context;

                _context = new ClientContext(_config.SPSettings.SiteUrl);

                _context.RequestTimeout = _config.SPSettings.RequestTimeout;
                _context.Credentials = new SharePointOnlineCredentials(_config.SPSettings.Login,
                    _config.SPSettings.Password.ToSecureString());
                _context.ExecuteQuery();

                return _context;
            }
        }

        #endregion

        #region :: Private Fields ::

        private const String TermsCategoriesCacheName = "TermsCategories";

        private readonly IConfluenceClient _confluenceClient;

        private readonly Regex ConfluenceImgSrcRegex = new Regex(@"\ssrc=""/wiki(?<src>[^\""]+)""");

        private readonly Regex ConfluenceDataBaseUrlRegex = new Regex(@"\sdata-base-url=""(?<src>[^\""]+)""");

        private readonly Regex tagRegex = new Regex("\\[.*\\]");

        private ClientContext _context;

        #endregion

        #region :: IDisposable ::

        private Boolean _disposed;

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(Boolean disposing)
        {
            if (!_disposed)
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
            _disposed = true;
        }

        #endregion
    }
}