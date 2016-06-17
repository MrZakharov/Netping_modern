using System;
using System.Web;
using HtmlAgilityPack;
using NetPing.Models;
using NetPing.PriceGeneration;
using NetPing.PriceGeneration.YandexMarker;
using NetPing_modern.DAL;

namespace NetPing.DAL
{
    internal class ProductCatalogManager
    {
        private readonly IRepository _dataRepository;

        public ProductCatalogManager(IRepository dataRepository)
        {
            _dataRepository = dataRepository;
        }

        public void GenerateYml()
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

            var tree = new DevicesTree(_dataRepository.Devices);

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

            shop.LocalDeliveryCost = 0;

            YmlGenerator.Generate(catalog, StaticFilePaths.CatalogFilePath);
        }

        //private static String GetDeviceUrl(Device device)
        //{
        //    return "http://www.netping.ru/products/" + device.Url;
        //}

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

            var deviceUrl = UrlBuilder.GetDeviceUrl(offerNode.Device.Url).ToString();

            bool stock = true;
            if (offerNode.Device.DeviceStock<=0)
            {
                stock = false;
            }

            shop.Offers.Add(new Offer
            {
                Id = offerNode.Id,
                Url = deviceUrl,
                Price = (Int32)(offerNode.Device.Price.HasValue ? offerNode.Device.Price.Value : 0),
                CategoryId = childCategoryNode.Id,
                Picture = offerNode.Device.GetCoverPhoto(true).Url,
                TypePrefix = "",
                VendorCode = offerNode.Name,
                Model = offerNode.Name,
                Store = stock,
                Description = descr
            });
        }
    }
}