using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using NetpingHelpers;

namespace NetPing.Models
{
    /// <summary>
    /// Store information of device
    /// </summary>
    [Serializable]
    public class Device
    {
        public int Id { get; set; }
        public string OldKey { get; set; }
        public SPTerm Name { get; set; }
        public List<SPTerm> Destination { get; set; }
        public double? Price { get; set; }
        public string GetCurrency
        { get
            {
                if (Helpers.IsCultureEng) return "usd";
                return "руб.";
            }
        }
        public SPTerm Label { get; set; }
        public DateTime Created { get; set; }
        public List<SPTerm> Connected_devices { get; set; }
        public string Url;

        public List<Post> Posts { get; set; }
        public List<SFile> SFiles { get; set; }
        public List<DeviceParameter> DeviceParameters { get; set; }

        public List<DevicePhoto> DevicePhotos { get; set; }

        public Device()
        {
            SFiles = new List<SFile>();
            Posts = new List<Post>();
        }

        public string Short_description { get; set; }

        public string Long_description { get; set; }

        public bool IsInArchive
        {
            get
            {
                if (Label != null && Label.OwnNameFromPath == "Archive")
                    return true;
                return false;
            }
        }

        public DevicePhoto GetCoverPhoto(bool isBig)
        {
            DevicePhoto dp=DevicePhotos.FirstOrDefault(p => p.IsBig == isBig && p.IsCover);
            if (dp != null) return dp;
            return new DevicePhoto();
        }

        /// <summary>
        /// The number of products in stock
        /// </summary>
        public int DeviceStock { get; set; }

        /// <summary>
        /// Date of information update about stock
        /// </summary>
        public DateTime DeviceStockUpdate { get; set; }
    }
}