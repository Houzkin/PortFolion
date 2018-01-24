using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using PortFolion.Core;
using System.Net;

namespace PortFolion.Web {
    /*
     * https://kabuoji3.com
     * */

    /*
     * 無尽蔵
     * http://souba-data.com/k_data/2018/18_01/T180104.zip
     * */

    public class DownloadSource {
        #region map
        class MjzMap : CsvClassMap<StockInfo> {
            public MjzMap() {
                Map(m => m.Symbol).Index(1);//
                Map(m => m.Name).Index(3);//
                Map(m => m.Open).Index(4);
                Map(m => m.High).Index(5);
                Map(m => m.Low).Index(6);
                Map(m => m.Close).Index(7);
                Map(m => m.Turnover).Index(8);
                Map(m => m.Market).Index(9);
            }
        }
        class SymbolConverter : DefaultTypeConverter {

        }
        class TickerConverter : DefaultTypeConverter {

        }
        #endregion

        #region instance
        private DownloadSource() { }
        #endregion

        public static DownloadSource Create(DateTime date) => new DownloadSource();

        public static IEnumerable<StockInfo> AcqireStockInfo(DateTime date,int start = 0,int end = 10) {
            throw new NotImplementedException();
        }
        static bool isDownloadable(FileInfo fi) {
            return (!fi.Exists);
        }
        static IEnumerable<StockInfo> getStockData(DateTime date) {
            throw new NotImplementedException();
        }
        static WebClient wc = new WebClient() { Encoding = Encoding.Default };
        static object _download(DateTime dt) {
            throw new NotImplementedException();
        }
    }
}
