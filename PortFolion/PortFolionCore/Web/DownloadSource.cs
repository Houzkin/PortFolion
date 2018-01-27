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
using Houzkin;
using PortFolion.IO;
using System.IO.Compression;

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

        static DateTime DayOfWeekSkip(DateTime date) {
            switch (date.DayOfWeek) {
                case DayOfWeek.Sunday:
                return date.AddDays(-2);
                case DayOfWeek.Saturday:
                return date.AddDays(-1);
                default:
                return date;
            }
        }
        public static DownloadSource Create(DateTime date) => new DownloadSource();

        public static IEnumerable<StockInfo> AcqireStockInfo(DateTime date,int start = 0,int end = 10) {
            //throw new NotImplementedException();
            for (int i=-1*start; i > -1 * end; i--) {
                date = DayOfWeekSkip(date.AddDays(i));
                var stockInfos = getStockData(date);
                if (stockInfos.Any()) {
                    foreach(var si in stockInfos) {
                        si.Date = date.Date;
                        yield return si;
                    }
                    yield break;
                }
            }
            yield break;
        }
        static bool isDownloadable(FileInfo fi) {
            return (!fi.Exists);
        }
        static IEnumerable<StockInfo> getStockData(DateTime date) {
            string[] dwPath = { "mujinzo", "zips", date.ToString("yyMMdd") };
            string[] acPath = { "mujinzo", "acv" , date.ToString("yyMMdd") };

            var fi = CacheManager.GetFileInfo(dwPath);
            if (!isDownloadable(fi))
                return Enumerable.Empty<StockInfo>();
            var rwv = _download(date, fi);
            if (!rwv.Result)
                return Enumerable.Empty<StockInfo>();

            throw new NotImplementedException();
        }
        static WebClient wc = new WebClient() { Encoding = Encoding.Default };
        static ResultWithValue<FileInfo> _download(DateTime dt,FileInfo dwnFi) {
            string url = "http://souba-data.com/k_data/"
                + dt.ToString("yyyy") + "/"
                + dt.ToString("yy_MM") + "/T"
                + dt.ToString("yyMMdd") + ".zip";
            var fileName = dwnFi.FullName;
            try {
                wc.DownloadFile(url, fileName);
                return new ResultWithValue<FileInfo>(dwnFi);
            } catch {
                return new ResultWithValue<FileInfo>();
            }
        }
        static ResultWithValue<FileInfo> _zipper(string acvPath, FileInfo dwnFi) {
            using (ZipArchive zar = ZipFile.OpenRead(dwnFi.FullName)) {
                var tgt = zar.Entries
                    .Where(e => e.FullName.EndsWith("csv", StringComparison.OrdinalIgnoreCase));
                foreach (var zae in tgt) {
                    //書き出し

                }
            }
            throw new NotImplementedException();
        }
    }
}
