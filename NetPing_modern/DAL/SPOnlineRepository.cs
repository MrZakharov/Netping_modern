using System;
using System.Collections.Generic;
using System.Linq;
using NLog;

using NetpingHelpers;
using NetPing.Models;
using NetPing_modern.DAL.Model;
using NetPing_modern.Services.Confluence;

namespace NetPing.DAL
{
    internal class SPOnlineRepository : IRepository
    {
        private readonly CachingProxy _dataProxy;

        private readonly DataStorageUpdater _dataUpdater;

        private readonly ProductCatalogManager _productCatalogManager;

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Repository);

        public SPOnlineRepository(IConfluenceClient confluenceClient, ISharepointClientFactory sharepointClientFactory)
        {
          

            try
            {
                var storage = new FileDataStorage();

                _dataProxy = new CachingProxy(storage);

                _dataUpdater = new DataStorageUpdater(storage, sharepointClientFactory, confluenceClient);

                _productCatalogManager = new ProductCatalogManager(this);

                //Log.Trace($"Instance of '{nameof(SPOnlineRepository)}' was created");
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
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.CurrentCulture;
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.CurrentUICulture;

            try
            {
                var isCultureRus = Helpers.IsCultureRus;

                if (name == "GenerateYml")
                {
                    Log.Trace($"GenerateYml action requested. IsCultureRus: {isCultureRus}");

                    if (isCultureRus)
                    {
                        _productCatalogManager.GenerateYml();
                    }
                }
                else if (name == "PushAll")
                {
                    _dataUpdater.Update();
                    if (isCultureRus)
                    {
                        _productCatalogManager.GenerateYml();
                    }

                }
                else
                {
                   // _dataUpdater.LoadDevices();
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
                Log.Trace($"Filtered devices collection was requested. ID: {id} Group: {groupId}");

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

                // Log.Trace($"Filtered devices collection was returned. ID: {id} Group: {groupId}");

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
                Log.Trace("Devices tree build was requested");

                var tree = new TreeNode<Device>(root);

                DevicesTreeHelper.BuildTree(tree, devices);

                Log.Trace("Devices tree was built");

                return tree;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Create devices tree error");

                throw;
            }
        }

        public UserManualModel GetUserManual(String id)
        {
            try
            {
                Log.Trace($"Requested user manual '{id}'");

                var manuals = _dataProxy.GetAndCache<UserManualModel>(new StorageKey()
                {
                    Name = id,
                    Directory = FileDataStorage.UserGuidFolder
                });

                var manual = manuals.FirstOrDefault();

                Log.Trace($"User manual '{id}' was found");

                return manual;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Get user manual error");

                throw;
            }
        }

        #endregion

        #region :: Public Properties ::

        public IEnumerable<Post> Posts => GetCollection<Post>(CacheKeys.Posts);

        public IEnumerable<SFile> SFiles => GetCollection<SFile>(CacheKeys.SFiles);

        public IEnumerable<PubFiles> PubFiles => GetCollection<PubFiles>(CacheKeys.PubFiles);

        public IEnumerable<DevicePhoto> DevicePhotos => GetCollection<DevicePhoto>(CacheKeys.DevicePhotos);

        public IEnumerable<HTMLInjection> HtmlInjections => GetCollection<HTMLInjection>(CacheKeys.HtmlInjection);

        public IEnumerable<SPTerm> TermsLabels => GetCollection<SPTerm>(CacheKeys.Labels);

        public IEnumerable<SPTerm> TermsCategories => GetCollection<SPTerm>(CacheKeys.PostCategories);

        public IEnumerable<SPTerm> TermsDeviceParameters => GetCollection<SPTerm>(CacheKeys.DeviceParameterNames);
        
        public IEnumerable<SPTerm> TermsFileTypes => GetCollection<SPTerm>(CacheKeys.DocumentTypes);

        public IEnumerable<SPTerm> TermsDestinations => GetCollection<SPTerm>(CacheKeys.Purposes);

        public IEnumerable<SPTerm> Terms => GetCollection<SPTerm>(CacheKeys.Names);

        public IEnumerable<SiteText> SiteTexts => GetCollection<SiteText>(CacheKeys.SiteTexts);

        public IEnumerable<DeviceParameter> DevicesParameters => GetCollection<DeviceParameter>(CacheKeys.DeviceParameters);

        public IEnumerable<Device> Devices => GetCollection<Device>(CacheKeys.Devices);

        #endregion

        private IEnumerable<T> GetCollection<T>(String name)
        {
            try
            {
                // Log.Trace($"Requested repository collection '{name}'. Item type: {typeof(T)}");

                var collection = _dataProxy.GetAndCache<T>(name);

                //Log.Trace($"Collection was returned. Name: '{name}' Item type: {typeof(T)}");

                return collection;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Unable to get collection '{name}'. Item type: {typeof(T)}");

                throw;
            }
        }
    }
}