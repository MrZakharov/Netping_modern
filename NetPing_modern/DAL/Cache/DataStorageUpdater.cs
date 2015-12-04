using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualBasic.FileIO;
using NetPing.Models;
using NetPing_modern.DAL.Model;
using NetPing_modern.Resources;
using NetPing_modern.Services.Confluence;
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

        private Dictionary<String, Action> _updateActions = new Dictionary<String, Action>(); 

        public DataStorageUpdater(IDataStorage storage, ISharepointClientFactory sharepointClientFactory, IConfluenceClient confluenceClient)
        {
            _storage = storage;
            _sharepointClientFactory = sharepointClientFactory;
            _confluenceClient = confluenceClient;

            InitUpdateActions();
        }

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
                Debug.WriteLine("Starting load data");

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

                Debug.WriteLine($"Step 1 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion
                
                #region :: Step 2 ::

                Parallel.Invoke(
                    LoadDevicePhotos,
                    LoadDeviceParameters,
                    LoadPubFiles,
                    LoadPosts,
                    LoadDeviceManualFiles);
                
                Debug.WriteLine($"Step 2 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion

                #region :: Step 3 ::

                LoadFirmwareFiles();

                LoadDevices();

                #endregion

                loadTimeMeasurer.Stop();
                
                Debug.WriteLine($"Step 3 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Load error");
            }
        }

        private Int32 IncreaseLoadCounter()
        {
            return Interlocked.Increment(ref _loadedModulesCounter);
        }

        public void LoadSFiles()
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
            catch
            {
            }

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Firmware files loaded. {counter}/{_totalModules}");
        }

        private void LoadDeviceManualFiles()
        {
            var deviceManualFileConverter = new DeviceManualFileConverter(_confluenceClient, _storage.GetNames(), _storage.GetDocumentTypes(), ManualSaver);

            LoadSharepointList(CacheKeys.SFiles, Camls.DeviceManual, deviceManualFileConverter, SharepointKeys.DeviceManualFiles);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Device manual fiels loaded. {counter}/{_totalModules}");
        }

        private void ManualSaver(UserManualModel userManualModel)
        {
            var name = userManualModel.Title.Replace("/","");

            _storage.Set(new StorageKey()
            {
                Name = name,
                Directory = InFileDataStorage.UserGuidFolder
            }, new [] {userManualModel});
        }

        public void LoadPosts()
        {
            LoadSharepointList(CacheKeys.Posts, Camls.Posts, new PostConverter(_confluenceClient, _storage.GetNames(), _storage.GetPostCategories()));

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Posts loaded. {counter}/{_totalModules}");
        }

        public void LoadPubFiles()
        {
            LoadSharepointList(CacheKeys.PubFiles, Camls.PubFiles, new PubFilesConverter(_storage.GetDocumentTypes()));

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Pub files loaded. {counter}/{_totalModules}");
        }

        public void LoadDeviceParameters()
        {
            LoadSharepointList(CacheKeys.DeviceParameters, String.Empty, new DeviceParameterConverter(_storage.GetDeviceParameterNames(), _storage.GetNames()));

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Device parameters loaded. {counter}/{_totalModules}");
        }

        public void LoadDevicePhotos()
        {
            LoadSharepointList(CacheKeys.DevicePhotos, Camls.DevicePhotos, new DevicePhotoConverter(_storage.GetNames()));

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Device photos loaded. {counter}/{_totalModules}");
        }

        public void LoadSiteTexts()
        {
            LoadSharepointList(CacheKeys.SiteTexts, Camls.SiteTexts, new SiteTextConverter(_confluenceClient));

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Site texts loaded. {counter}/{_totalModules}");
        }

        public void LoadHtmlInjections()
        {
            LoadSharepointList(CacheKeys.HtmlInjection, Camls.HtmlInjection, new HtmlInjectionConverter());

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Html injections loaded. {counter}/{_totalModules}");
        }

        public void LoadDocumentTypeTerms()
        {
            LoadSharepointTerms(CacheKeys.DocumentTypes);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Sharepoint document type terms loaded. {counter}/{_totalModules}");
        }

        public void LoadPostCategoryTerms()
        {
            LoadSharepointTerms(CacheKeys.PostCategories);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Sharepoint post category terms loaded. {counter}/{_totalModules}");
        }

        public void LoadLabelTerms()
        {
            LoadSharepointTerms(CacheKeys.Labels);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Label terms loaded. {counter}/{_totalModules}");
        }

        public void LoadDeviceParameterTerms()
        {
            LoadSharepointTerms(CacheKeys.DeviceParameterNames);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Sharepoint device parameter terms loaded. {counter}/{_totalModules}");
        }

        public void LoadPurposeTerms()
        {
            LoadSharepointTerms(CacheKeys.Purposes);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Sharepoint purpose terms loaded. {counter}/{_totalModules}");
        }

        public void LoadNameTerms()
        {
            LoadSharepointTerms(CacheKeys.Names);

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Sharepoint name terms loaded. {counter}/{_totalModules}");
        }

        private void LoadSharepointList<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            var convertedList = GetSharepointList(listName, query, converter, sharepointName);

            _storage.Set(listName, convertedList);
        }

        private void LoadSharepointListParallel<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            var convertedList = GetSharepointListParallel(listName, query, converter, sharepointName);

            _storage.Set(listName, convertedList);
        }

        private void LoadSharepointListAsAppend<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            var convertedList = GetSharepointList(listName, query, converter, sharepointName);

            _storage.Append(listName, convertedList);
        }

        private IEnumerable<T> GetSharepointList<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
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

        private IEnumerable<T> GetSharepointListParallel<T>(String listName, String query, IListItemConverter<T> converter, String sharepointName = null)
        {
            using (var sp = _sharepointClientFactory.Create())
            {
                if (sharepointName == null)
                {
                    sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(listName);
                }

                var items = sp.GetList(sharepointName, query);

                var results = new List<T>();

                Parallel.ForEach(items, (item) =>
                {
                    var convertedItem = converter.Convert(item);
                    
                    if (convertedItem != null)
                    {
                        results.Add(convertedItem);
                    }
                });

                var convertedList = items.ToList().Select(converter.Convert).Where(i => i != null).ToList();

                return convertedList;
            }
        }

        private void LoadSharepointTerms(String setName)
        {
            using (var sp = _sharepointClientFactory.Create())
            {
                var sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(setName);

                var nameTerms = sp.GetTerms(sharepointName);

                _storage.Set(setName, nameTerms);
            }
        }

        public void LoadDevices()
        {
            var posts = _storage.Get<Post>(CacheKeys.Posts);
            var files = _storage.Get<SFile>(CacheKeys.SFiles);
            var photos = _storage.Get<DevicePhoto>(CacheKeys.DevicePhotos);
            var devicesParameters = _storage.Get<DeviceParameter>(CacheKeys.DeviceParameters);
            var names = _storage.GetNames();
            var purposes = _storage.Get<SPTerm>(CacheKeys.Purposes);
            var labels = _storage.Get<SPTerm>(CacheKeys.Labels);
            
            var stockCsv = HttpContext.Current.Server.MapPath("~/Pub/Data/netping_ru_stock.csv");

            var dataTable = new Dictionary<String, String>();

            var deviceStockUpdate = new DateTime();

            if (File.Exists(stockCsv))
            {
                dataTable = GetDataTableFromCSVFile(stockCsv);
                deviceStockUpdate = DateTime.Parse(dataTable[""]);
            }

            var deviceConverter = new DeviceConverter(_confluenceClient, dataTable, names,purposes,labels,deviceStockUpdate);

            var devices = GetSharepointList(CacheKeys.Devices, Camls.CamlDevices, deviceConverter).ToList();


            foreach (var dev in devices)
            {
                dev.Posts =
                    posts.Where(
                        pst => dev.Name.IsIncludeAnyFromOthers(pst.Devices) || dev.Name.IsUnderAnyOthers(pst.Devices))
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

            var counter = IncreaseLoadCounter();

            Debug.WriteLine($"Devices loaded. {counter}/{_totalModules}");
        }

        private Dictionary<Device, Int32> BuildIndex(IEnumerable<Device> devices, List<SPTerm> names)
        {
            var result = new Dictionary<Device, Int32>();

            foreach (var device in devices)
            {
                var i = FindIndex(names, n => n.Id == device.Name.Id);

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
    }
}