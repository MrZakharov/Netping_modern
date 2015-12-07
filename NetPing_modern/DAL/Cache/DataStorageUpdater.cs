using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic.FileIO;
using NetPing.Models;
using NetPing_modern.DAL;
using NetPing_modern.DAL.Model;
using NetPing_modern.Resources;
using NetPing_modern.Services.Confluence;
using NLog;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class DataStorageUpdater
    {
        private readonly IDataStorage _storage;
        private readonly ISharepointClientFactory _sharepointClientFactory;
        private readonly IConfluenceClient _confluenceClient;

        private Int32 _loadedModulesCounter = 0;
        private Int32 _totalModules = 15;

        private readonly Dictionary<String, Action> _updateActions = new Dictionary<String, Action>();

        private static readonly Logger Log = LogManager.GetLogger(LogNames.Loader);

        public DataStorageUpdater(IDataStorage storage, ISharepointClientFactory sharepointClientFactory, IConfluenceClient confluenceClient)
        {
            _storage = storage;
            _sharepointClientFactory = sharepointClientFactory;
            _confluenceClient = confluenceClient;

            InitUpdateActions();

            Log.Trace($"Instance of {nameof(DataStorageUpdater)} was created");
        }

        public Action GetUpdateActionByKey(String key)
        {
            if (!_updateActions.ContainsKey(key))
            {
                return null;
            }

            var action = _updateActions[key];

            return action;
        }

        public void Update()
        {
            try
            {
                Log.Trace("Start data loading");
                
                var loadTimeMeasurer = Stopwatch.StartNew();

                #region :: Step 1 ::

                Parallel.Invoke(
                    LoadNameTerms,
                    LoadPurposeTerms,
                    LoadDeviceParameterTerms,
                    LoadLabelTerms,
                    LoadPostCategoryTerms,
                    LoadDocumentTypeTerms,
                    LoadHtmlInjections,
                    LoadSiteTexts);

                Log.Trace($"Step 1 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion
                
                #region :: Step 2 ::

                Parallel.Invoke(
                    LoadDevicePhotos,
                    LoadDeviceParameters,
                    LoadPubFiles,
                    LoadPosts,
                    LoadDeviceManualFiles);

                Log.Trace($"Step 2 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion

                #region :: Step 3 ::

                LoadFirmwareFiles();

                LoadDevices();

                #endregion

                loadTimeMeasurer.Stop();

                Log.Trace($"Step 3 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Load data error");

                throw;
            }
        }

        #region :: Load Methods ::

        private void LoadDevices()
        {
            try
            {
                var posts = _storage.Get<Post>(CacheKeys.Posts).ToList();
                var files = _storage.Get<SFile>(CacheKeys.SFiles).ToList();
                var photos = _storage.Get<DevicePhoto>(CacheKeys.DevicePhotos).ToList();
                var devicesParameters = _storage.Get<DeviceParameter>(CacheKeys.DeviceParameters).ToList();
                var names = _storage.GetNames();
                var purposes = _storage.Get<SPTerm>(CacheKeys.Purposes);
                var labels = _storage.Get<SPTerm>(CacheKeys.Labels);

                var stockCsv = StaticFilePaths.StockFilePath;

                var dataTable = new Dictionary<String, String>();

                var deviceStockUpdate = new DateTime();

                if (File.Exists(stockCsv))
                {
                    dataTable = GetDataTableFromCSVFile(stockCsv);
                    deviceStockUpdate = DateTime.Parse(dataTable[String.Empty]);
                }

                var deviceConverter = new DeviceConverter(_confluenceClient, dataTable, names, purposes, labels,
                    deviceStockUpdate);

                var devices = GetSharepointList(CacheKeys.Devices, Camls.CamlDevices, deviceConverter).ToList();

                foreach (var dev in devices)
                {
                    dev.Posts =
                        posts.Where(
                            pst =>
                                dev.Name.IsIncludeAnyFromOthers(pst.Devices) || dev.Name.IsUnderAnyOthers(pst.Devices))
                            .ToList();
                    dev.SFiles =
                        files.Where(
                            fl => dev.Name.IsIncludeAnyFromOthers(fl.Devices) || dev.Name.IsUnderAnyOthers(fl.Devices))
                            .ToList();

                    // collect device parameters 
                    dev.DeviceParameters = devicesParameters.Where(par => par.Device == dev.Name).ToList();

                    // Get device photos
                    dev.DevicePhotos = photos.Where(p => p.Dev_name.Id == dev.Name.Id).ToList();
                }

                var index = BuildIndex(devices, names.ToList());

                var sortedDevices = devices.OrderBy(d => index[d]).ToList();

                _storage.Set(CacheKeys.Devices, sortedDevices);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load devices");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Devices loaded. {counter}/{_totalModules}");
        }

        private void LoadSFiles()
        {
            LoadDeviceManualFiles();
            LoadFirmwareFiles();
        }

        private void LoadFirmwareFiles()
        {
            try
            {
                using (var sp = _sharepointClientFactory.Create())
                {
                    var firmwareFileConverter = new FirmwareFileConverter(sp, _storage.GetNames(),
                        _storage.GetDocumentTypes());

                    LoadSharepointListAsAppend(CacheKeys.SFiles, Camls.FirmwareFiles, firmwareFileConverter,
                        SharepointKeys.FirmwareFiles);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load firmwares");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Firmware files loaded. {counter}/{_totalModules}");
        }

        private void LoadDeviceManualFiles()
        {
            try
            {
                var deviceManualFileConverter = new DeviceManualFileConverter(_confluenceClient, _storage.GetNames(),
                    _storage.GetDocumentTypes(), ManualSaver);

                LoadSharepointList(CacheKeys.SFiles, Camls.DeviceManual, deviceManualFileConverter,
                    SharepointKeys.DeviceManualFiles);


            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load manual files");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Device manual fiels loaded. {counter}/{_totalModules}");
        }

        private void LoadPosts()
        {
            try
            {
                LoadSharepointList(CacheKeys.Posts, Camls.Posts,
                    new PostConverter(_confluenceClient, _storage.GetNames(), _storage.GetPostCategories()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load posts");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Posts loaded. {counter}/{_totalModules}");
        }

        private void LoadPubFiles()
        {
            try
            {
                LoadSharepointList(CacheKeys.PubFiles, Camls.PubFiles,
                    new PubFilesConverter(_storage.GetDocumentTypes()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load pub files");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Pub files loaded. {counter}/{_totalModules}");
        }

        private void LoadDeviceParameters()
        {
            try
            {
                LoadSharepointList(CacheKeys.DeviceParameters, String.Empty,
                    new DeviceParameterConverter(_storage.GetDeviceParameterNames(), _storage.GetNames()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load device parameters");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Device parameters loaded. {counter}/{_totalModules}");
        }

        private void LoadDevicePhotos()
        {
            try
            {
                LoadSharepointList(CacheKeys.DevicePhotos, Camls.DevicePhotos,
                    new DevicePhotoConverter(_storage.GetNames()));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load device photos");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Device photos loaded. {counter}/{_totalModules}");
        }

        private void LoadSiteTexts()
        {
            try
            {
                LoadSharepointList(CacheKeys.SiteTexts, Camls.SiteTexts, new SiteTextConverter(_confluenceClient));
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load site texts");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Site texts loaded. {counter}/{_totalModules}");
        }

        private void LoadHtmlInjections()
        {
            try
            {
                LoadSharepointList(CacheKeys.HtmlInjection, Camls.HtmlInjection, new HtmlInjectionConverter());
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load html injections");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Html injections loaded. {counter}/{_totalModules}");
        }

        private void LoadDocumentTypeTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.DocumentTypes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load document type terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Sharepoint document type terms loaded. {counter}/{_totalModules}");
        }

        private void LoadPostCategoryTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.PostCategories);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load post category terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Sharepoint post category terms loaded. {counter}/{_totalModules}");
        }

        private void LoadLabelTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.Labels);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load label terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Label terms loaded. {counter}/{_totalModules}");
        }

        private void LoadDeviceParameterTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.DeviceParameterNames);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to loade device parameter terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Sharepoint device parameter terms loaded. {counter}/{_totalModules}");
        }

        private void LoadPurposeTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.Purposes);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load purpose terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Sharepoint purpose terms loaded. {counter}/{_totalModules}");
        }

        private void LoadNameTerms()
        {
            try
            {
                LoadSharepointTerms(CacheKeys.Names);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load name terms");
            }

            var counter = IncreaseLoadCounter();

            Log.Trace($"Sharepoint name terms loaded. {counter}/{_totalModules}");
        }

        #endregion

        private void InitUpdateActions()
        {
            _updateActions.Add(CacheKeys.DocumentTypes, LoadDocumentTypeTerms);

            _updateActions.Add(CacheKeys.Names, LoadNameTerms);

            _updateActions.Add(CacheKeys.SiteTexts, LoadSiteTexts);

            _updateActions.Add(CacheKeys.Labels, LoadLabelTerms);

            _updateActions.Add(CacheKeys.DeviceParameterNames, LoadDeviceParameterTerms);

            _updateActions.Add(CacheKeys.PostCategories, LoadPostCategoryTerms);

            _updateActions.Add(CacheKeys.Purposes, LoadPurposeTerms);

            _updateActions.Add(CacheKeys.DeviceParameters, LoadDeviceParameters);

            _updateActions.Add(CacheKeys.DevicePhotos, LoadDevicePhotos);

            _updateActions.Add(CacheKeys.PubFiles, LoadPubFiles);

            _updateActions.Add(CacheKeys.SFiles, LoadSFiles);

            _updateActions.Add(CacheKeys.Posts, LoadPosts);

            _updateActions.Add(CacheKeys.Devices, LoadDevices);
        }

        private Int32 IncreaseLoadCounter()
        {
            return Interlocked.Increment(ref _loadedModulesCounter);
        }

        private void ManualSaver(UserManualModel userManualModel)
        {
            try
            {
                var name = userManualModel.Title.Replace("/", "");

                _storage.Set(new StorageKey()
                {
                    Name = name,
                    Directory = FileDataStorage.UserGuidFolder
                }, new[] { userManualModel });
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to save manual model");

                throw;
            }
        }

        private void LoadSharepointList<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            var convertedList = GetSharepointList(listName, query, converter, sharepointName);

            _storage.Set(listName, convertedList);
        }

        private void LoadSharepointListAsAppend<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            var convertedList = GetSharepointList(listName, query, converter, sharepointName);

            _storage.Append(listName, convertedList);
        }

        private IEnumerable<T> GetSharepointList<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            try
            {
                using (var sp = _sharepointClientFactory.Create())
                {
                    if (sharepointName == null)
                    {
                        sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(listName);
                    }

                    var items = sp.GetList(sharepointName, query);

                    var convertedList = items.ToList().Select(converter.Convert).Where(i => i != null).ToList();

                    return convertedList;
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to get sharepoint list");

                throw;
            }
        }

        private void LoadSharepointTerms(String setName)
        {
            try
            {
                using (var sp = _sharepointClientFactory.Create())
                {
                    var sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(setName);

                    var nameTerms = sp.GetTerms(sharepointName);

                    _storage.Set(setName, nameTerms);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Unable to load sharepoint terms");

                throw;
            }
        }
        
        private Dictionary<Device, Int32> BuildIndex(IEnumerable<Device> devices, List<SPTerm> names)
        {
            try
            {
                var result = new Dictionary<Device, Int32>();

                foreach (var device in devices)
                {
                    var i = FindIndex(names, n => n.Id == device.Name.Id);

                    result.Add(device, i > -1 ? i : Int32.MaxValue);
                }

                return result;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Build devices index error");

                throw;
            }
        }

        private static Int32 FindIndex<T>(IEnumerable<T> items, Func<T, Boolean> predicate)
        {
            var index = 0;

            foreach (var item in items)
            {
                if (predicate(item))
                {
                    return index;
                }

                index++;
            }

            return -1;
        }

        private Dictionary<String, String> GetDataTableFromCSVFile(String csvFilePath)
        {
            var csvData = new Dictionary<String, String>();

            try
            {
                using (var csvReader = new TextFieldParser(csvFilePath))
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
                Log.Warn(ex, $"Unable to load stock data from csv file '{csvFilePath}'"); 
            }

            return csvData;
        }
    }
}