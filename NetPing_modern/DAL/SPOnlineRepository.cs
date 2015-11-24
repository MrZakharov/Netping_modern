using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using NetpingHelpers;
using NetPing.Models;
using NetPing.Tools;
using NetPing_modern.DAL.Model;
using NetPing_modern.Services.Confluence;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class SPOnlineRepository : IRepository
    {
        private readonly CachingProxy _dataProxy;

        private readonly DataStorageUpdater _dataUpdater;

        private readonly ProductCatalogManager _productCatalogManager;

        public SPOnlineRepository(IConfluenceClient confluenceClient, ISharepointClientFactory sharepointClientFactory)
        {
            var storage = new InFileDataStorage();
            
            _dataProxy = new CachingProxy(storage);

            _dataUpdater = new DataStorageUpdater(storage, sharepointClientFactory, confluenceClient);

            _productCatalogManager = new ProductCatalogManager(this);
        }

        #region :: Public Methods ::

        public String UpdateAll()
        {
            try
            {
                _dataUpdater.Update();

                Debug.WriteLine("PushToCache OK");

                if (Helpers.IsCultureRus)
                {
                    _productCatalogManager.GenerateYml();
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
            switch (name)
            {
                case CacheKeys.DocumentTypes:
                    _dataUpdater.LoadDocumentTypeTerms();
                    break;
                case CacheKeys.Names:
                    _dataUpdater.LoadNameTerms();
                    break;
                case CacheKeys.SiteTexts:
                    _dataUpdater.LoadSiteTexts();
                    break;
                case CacheKeys.Labels:
                    _dataUpdater.LoadLabelTerms();
                    break;
                case CacheKeys.DeviceParameterNames:
                    _dataUpdater.LoadDeviceParameterTerms();
                    break;
                case CacheKeys.PostCategories:
                    _dataUpdater.LoadPostCategoryTerms();
                    break;
                case CacheKeys.Purposes:
                    _dataUpdater.LoadPurposeTerms();
                    break;
                case CacheKeys.DeviceParameters:
                    _dataUpdater.LoadDeviceParameters();
                    break;
                case CacheKeys.DevicePhotos:
                    _dataUpdater.LoadDevicePhotos();
                    break;
                case CacheKeys.PubFiles:
                    _dataUpdater.LoadPubFiles();
                    break;
                case CacheKeys.SFiles:
                    _dataUpdater.LoadSFiles();
                    break;
                case CacheKeys.Posts:
                    _dataUpdater.LoadPosts();
                    break;
                case CacheKeys.Devices:
                    _dataUpdater.LoadDevices();
                    break;
                case "GenerateYml":
                    if (Helpers.IsCultureRus)
                    {
                        _productCatalogManager.GenerateYml();
                    }
                    break;
                case "PushAll":
                {
                    _dataUpdater.LoadDocumentTypeTerms();
                    _dataUpdater.LoadNameTerms();
                    _dataUpdater.LoadSiteTexts();
                    _dataUpdater.LoadLabelTerms();
                    _dataUpdater.LoadDeviceParameterTerms();
                    _dataUpdater.LoadPostCategoryTerms();
                    _dataUpdater.LoadPurposeTerms();
                    _dataUpdater.LoadDeviceParameters();
                    _dataUpdater.LoadDevicePhotos();
                    _dataUpdater.LoadPubFiles();
                    _dataUpdater.LoadSFiles();
                    _dataUpdater.LoadPosts();

                    break;
                }
                default:
                    return "404";
            }

            return "OK";
        }

        public IEnumerable<Device> GetDevices(String id, String groupId)
        {
            if (String.IsNullOrEmpty(id))
                throw new ArgumentNullException(nameof(id));

            if (String.IsNullOrEmpty(groupId))
                throw new ArgumentNullException(nameof(groupId));

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

                var fileName = HttpContext.Current.Server.MapPath("~/Content/Data/UserGuides/" + model.Title.Replace("/", "") + "_" +
                                                       CultureInfo.CurrentCulture.IetfLanguageTag + ".dat");
                Stream streamWrite = null;
                try
                {
                    streamWrite = File.Create(fileName);
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
        
        public static String GetDeviceUrl(Device device)
        {
            return "http://www.netping.ru/products/" + device.Url;
        }

        #endregion

        #region :: Public Properties ::

        public IEnumerable<Post> Posts => _dataProxy.GetAndCache<Post>(CacheKeys.Posts);

        public IEnumerable<SFile> SFiles => _dataProxy.GetAndCache<SFile>(CacheKeys.SFiles);

        public IEnumerable<PubFiles> PubFiles => _dataProxy.GetAndCache<PubFiles>(CacheKeys.PubFiles);

        public IEnumerable<DevicePhoto> DevicePhotos => _dataProxy.GetAndCache<DevicePhoto>(CacheKeys.DevicePhotos);

        public IEnumerable<HTMLInjection> HtmlInjections => _dataProxy.GetAndCache<HTMLInjection>(CacheKeys.HtmlInjection);

        public IEnumerable<SPTerm> TermsLabels => _dataProxy.GetAndCache<SPTerm>(CacheKeys.Labels);

        public IEnumerable<SPTerm> TermsCategories => _dataProxy.GetAndCache<SPTerm>(CacheKeys.PostCategories);

        public IEnumerable<SPTerm> TermsDeviceParameters => _dataProxy.GetAndCache<SPTerm>(CacheKeys.DeviceParameterNames);
        
        public IEnumerable<SPTerm> TermsFileTypes => _dataProxy.GetAndCache<SPTerm>(CacheKeys.DocumentTypes);

        public IEnumerable<SPTerm> TermsDestinations => _dataProxy.GetAndCache<SPTerm>(CacheKeys.Purposes);

        public IEnumerable<SPTerm> Terms => _dataProxy.GetAndCache<SPTerm>(CacheKeys.Names);

        public IEnumerable<SiteText> SiteTexts => _dataProxy.GetAndCache<SiteText>(CacheKeys.SiteTexts);

        public IEnumerable<DeviceParameter> DevicesParameters => _dataProxy.GetAndCache<DeviceParameter>(CacheKeys.DeviceParameters);

        public IEnumerable<Device> Devices => _dataProxy.GetAndCache<Device>(CacheKeys.Devices);

        #endregion

        #region :: Private Methods ::
        
        private void BuildTree(TreeNode<Device> dev, IEnumerable<Device> list)
        {
            var childrens = list.Where(d => d.Name.Level == dev.Value.Name.Level + 1 && d.Name.Path.Contains(dev.Value.Name.Path));

            foreach (var child in childrens)
            {
                BuildTree(dev.AddChild(child), list);
            }
        }

        #endregion
    }
}