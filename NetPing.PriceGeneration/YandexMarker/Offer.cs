using System;
using System.Xml.Serialization;

namespace NetPing.PriceGeneration.YandexMarker
{
    [Serializable]
    public class Offer
    {
        public Offer()
        {
            Type = "vendor.model";
            Available = true;
            Store = true;
            Pickup = true;
            Delivery = true;
            Vendor = "NetPing";
            SalesNotes = "Предоплата 100%. С вами свяжется менеджер.";
            ManufacturerWarranty = "P2Y";
            Country = "Россия";
            CurrencyId = "RUR";
        }

        [XmlAttribute("id")]
        public int Id { get; set; }

        [XmlAttribute("type")]
        public string Type
        {
            get; set;
        }

        [XmlAttribute("available")]
        public bool Available { get; set; }

        [XmlElement("url")]
        public string Url { get; set; }

        [XmlElement("price")]
        public int Price { get; set; }

        [XmlElement("currencyId")]
        public string CurrencyId { get; set; }

        [XmlElement("categoryId")]
        public int CategoryId { get; set; }

        [XmlElement("picture")]
        public string Picture { get; set; }

        [XmlElement("store")]
        public bool Store { get; set; }

        [XmlElement("pickup")]
        public bool Pickup { get; set; }

        [XmlElement("delivery")]
        public bool Delivery { get; set; }

        [XmlElement("typePrefix")]
        public string TypePrefix { get; set; }

        [XmlElement("vendor")]
        public string Vendor { get; set; }

        [XmlElement("vendorCode")]
        public string VendorCode { get; set; }

        [XmlElement("model")]
        public string Model { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("sales_notes")]
        public string SalesNotes { get; set; }

        [XmlElement("manufacturer_warranty")]
        public string ManufacturerWarranty { get; set; }

        [XmlElement("country_of_origin")]
        public string Country { get; set; }
    }
}
