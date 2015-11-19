using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Microsoft.VisualBasic.FileIO;
using NetPing.Models;
using NetPing_modern.Resources;
using NetPing_modern.Services.Confluence;
using File = System.IO.File;

namespace NetPing.DAL
{
    internal class InFileDataStorageSynchronizer
    {
        private readonly IDataStorage _storage;
        private readonly SharepointClientParameters _sharepointClientParameters;
        private readonly ConfluenceClient _confluenceClient;

        public InFileDataStorageSynchronizer(IDataStorage storage, SharepointClientParameters sharepointClientParameters, ConfluenceClient confluenceClient)
        {
            _storage = storage;
            _sharepointClientParameters = sharepointClientParameters;
            _confluenceClient = confluenceClient;
        }

        public void Load()
        {
            try
            {
                Debug.WriteLine("Starting load data");

                var loadTimeMeasurer = Stopwatch.StartNew();

                #region :: Step 1 ::

                Parallel.Invoke(
                    () => LoadSharepointTerms(CacheKeys.Names),
                    () => LoadSharepointTerms(CacheKeys.Purposes),
                    () => LoadSharepointTerms(CacheKeys.DeviceParameterNames),
                    () => LoadSharepointTerms(CacheKeys.Labels),
                    () => LoadSharepointTerms(CacheKeys.PostCategories),
                    () => LoadSharepointTerms(CacheKeys.DocumentTypes),
                    () => LoadSharepointList(CacheKeys.HtmlInjection, Camls.HtmlInjection, new HtmlInjectionConverter()),
                    () =>
                        LoadSharepointList(CacheKeys.SiteTexts, Camls.SiteTexts,
                            new SiteTextConverter(_confluenceClient)));

                Debug.WriteLine($"Step 1 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion

                var names = _storage.Get<SPTerm>(CacheKeys.Names).ToList();

                var fileTypeTerms = _storage.Get<SPTerm>(CacheKeys.DocumentTypes).ToList();

                #region :: Step 2 ::

                var devicePhotoConverter = new DevicePhotoConverter(names);
                var deviceParameterConverter =
                    new DeviceParameterConverter(_storage.Get<SPTerm>(CacheKeys.DeviceParameterNames), names);

                var pubFilesConverter = new PubFilesConverter(fileTypeTerms);
                var postConverter = new PostConverter(_confluenceClient, names,
                    _storage.Get<SPTerm>(CacheKeys.PostCategories));
                var deviceManualFileConverter = new DeviceManualFileConverter(_confluenceClient, names, fileTypeTerms);

                Parallel.Invoke(
                    () => LoadSharepointList(CacheKeys.DevicePhotos, Camls.DevicePhotos, devicePhotoConverter),
                    () => LoadSharepointList(CacheKeys.DeviceParameters, String.Empty, deviceParameterConverter),
                    () => LoadSharepointList(CacheKeys.PubFiles, Camls.PubFiles, pubFilesConverter),
                    () => LoadSharepointList(CacheKeys.Posts, Camls.Posts, postConverter),
                    () => LoadSharepointList(CacheKeys.SFiles, Camls.DeviceManual, deviceManualFileConverter, SharepointKeys.DeviceManualFiles));
                
                Debug.WriteLine($"Step 2 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                #endregion

                #region :: Step 3 ::

                using (var sp = new SharepointClient(_sharepointClientParameters))
                {
                    var firmwareFileConverter = new FirmwareFileConverter(sp, names, fileTypeTerms);

                    LoadSharepointListAsAppend(CacheKeys.SFiles, Camls.FirmwareFiles, firmwareFileConverter);
                }

                LoadDevices(_storage.Get<Post>(CacheKeys.Posts), _storage.Get<SFile>(CacheKeys.SFiles),
                    _storage.Get<DevicePhoto>(CacheKeys.DevicePhotos),
                    _storage.Get<DeviceParameter>(CacheKeys.DeviceParameters), names,
                    _storage.Get<SPTerm>(CacheKeys.Purposes), _storage.Get<SPTerm>(CacheKeys.Labels));

                #endregion

                loadTimeMeasurer.Stop();
                
                Debug.WriteLine($"Step 3 data loaded. From start: {loadTimeMeasurer.ElapsedMilliseconds} ms");

                int a = 0;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Load error");
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
            using (var sp = new SharepointClient(_sharepointClientParameters))
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

        private void LoadSharepointTerms(String setName)
        {
            using (var sp = new SharepointClient(_sharepointClientParameters))
            {
                var sharepointName = SharepointToCacheNamesMapper.SharepointKeyByCacheKey(setName);

                var nameTerms = sp.GetTerms(sharepointName);

                _storage.Set(setName, nameTerms);
            }
        }

        private void LoadDevices(
            IEnumerable<Post> posts, 
            IEnumerable<SFile> files, 
            IEnumerable<DevicePhoto> photos, 
            IEnumerable<DeviceParameter> devicesParameters,
            IEnumerable<SPTerm> names, 
            IEnumerable<SPTerm> purposes, 
            IEnumerable<SPTerm> labels)
        {

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

            _storage.Set(CacheKeys.Devices, devices);
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