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
using NLog;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class SPOnlineRepository : IRepository
    {
        private readonly CachingProxy _dataProxy;

        private readonly DataStorageUpdater _dataUpdater;

        private readonly ProductCatalogManager _productCatalogManager;

        private static readonly Logger Log = LogManager.GetLogger(LogNames.RepositoryLog);

        public SPOnlineRepository(IConfluenceClient confluenceClient, ISharepointClientFactory sharepointClientFactory)
        {
            try
            {
                var storage = new InFileDataStorage();

                _dataProxy = new CachingProxy(storage);

                _dataUpdater = new DataStorageUpdater(storage, sharepointClientFactory, confluenceClient);

                _productCatalogManager = new ProductCatalogManager(this);

                Log.Trace($"Instance of '{nameof(SPOnlineRepository)}' created");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"'{nameof(SPOnlineRepository)}' initializing failed");

                throw;
            }
        }

        #region :: Public Methods ::

        public String UpdateAll()
        {
            try
            {
                Log.Trace("Starting to update all data in storage");

                _dataUpdater.Update();

                if (Helpers.IsCultureRus)
                {
                    _productCatalogManager.GenerateYml();
                }

                Log.Trace("All data in storage updated");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Update all data in storage failed");

                return ex.ToString();
            }

            return "OK!";
        }

        public String UpdateAllAsync(String name)
        {
            Log.Trace($"Starting to execute update action with name '{name}'");
            
            try
            {
                if (name == "GenerateYml")
                {
                    var isCultureRus = Helpers.IsCultureRus;

                    Log.Trace($"GenerateYml action requested. IsCultureRus: {isCultureRus}");

                    if (isCultureRus)
                    {
                        _productCatalogManager.GenerateYml();
                    }
                }
                else if (name == "PushAll")
                {
                    _dataUpdater.Update();
                }
                else
                {
                    var updateAction = _dataUpdater.GetUpdateActionByKey(name);

                    if (updateAction != null)
                    {
                        updateAction();
                    }
                    else
                    {
                        Log.Warn($"Update action with name '{name}' does not exist");

                        return "404";
                    }
                }

                Log.Trace($"Execution of action with name'{name}' completed");
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Update action with name '{name}' failed");

                throw;
            }

            return "OK";
        }

        public IEnumerable<Device> GetDevices(String id, String groupId)
        {
            try
            {
                if (String.IsNullOrEmpty(id))
                {
                    throw new ArgumentNullException(nameof(id));
                }

                if (String.IsNullOrEmpty(groupId))
                {
                    throw new ArgumentNullException(nameof(groupId));
                }

                var group = Devices.FirstOrDefault(d => d.Url == groupId);

                if (group == null)
                {
                    throw new InvalidOperationException($"Unable to find group with url '{groupId}'");
                }

                var devices = Devices.Where(d => d.Name.IsUnderOther(group.Name) && !d.Name.IsGroup());

                return devices;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Get devices error");

                throw;
            }
        }

        public TreeNode<Device> DevicesTree(Device root, IEnumerable<Device> devices)
        {
            try
            {
                var tree = new TreeNode<Device>(root);

                DevicesTreeHelper.BuildTree(tree, devices);

                return tree;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Create devices tree error");

                throw;
            }
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
                    Log.Error(ex, "Save user guide to file error");

                    streamWrite?.Close();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Cache user guide error");
            }
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
    }
}