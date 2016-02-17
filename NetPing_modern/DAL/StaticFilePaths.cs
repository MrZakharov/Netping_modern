using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace NetPing_modern.DAL
{
    public class StaticFilePaths
    {
        private static readonly String BaseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        public static String StockFilePath => Path.Combine(BaseDirectory, "Pub\\Data\\netping_ru_stock.csv");

        public static String CatalogFilePath => Path.Combine(BaseDirectory, "Content/Data/netping.xml");
    }
}