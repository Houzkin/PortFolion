using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using PortFolion.Core;
using System.Net;
using Houzkin;
using PortFolion.IO;
using System.IO.Compression;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;
using System.Collections;

namespace PortFolion.Web {
    /*
     * https://kabuoji3.com
     * */

    /*
     * 無尽蔵
     * http://souba-data.com/k_data/2018/18_01/T180104.zip
     * */
    
    public class TickerTable : IEnumerable<StockInfo> {
        #region map
        
        class MjzMap : ClassMap<StockInfo> {
            public MjzMap() {
                Map(m => m.Symbol).Index(1).Default("0000");
                Map(m => m.Name).Index(3).TypeConverter<TickerConverter>().Default("");
                Map(m => m.Open).Index(4).Default(0);
                Map(m => m.High).Index(5).Default(0);
                Map(m => m.Low).Index(6).Default(0);
                Map(m => m.Close).Index(7).Default(0);
                Map(m => m.Turnover).Index(8).Default(0);
                Map(m => m.Market).Index(9).Default("");
            }
        }
        class SymbolConverter : DefaultTypeConverter {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
                var s = text.Split(' ').FirstOrDefault();
                int i;
                if(int.TryParse(s,out i)) {
                    return i.ToString();
                } else {
                    return "0000";
                }
            }
        }
        /// <summary>
        /// 取得したテキストから銘柄名を抽出します。
        /// </summary>
        class TickerConverter : DefaultTypeConverter {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
                var s = text.Split(' ').Skip(1);
                if (s.Any())
                    return s.First();
                else
                    return "";
            }
        }
        #endregion

        #region instance
        public IEnumerator<StockInfo> GetEnumerator() {
            return _infos.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator() {
            return _infos.GetEnumerator();
        }
        private TickerTable(DateTime date) {
            _infos = AcqireStockInfo(date);
        }
        private IEnumerable<StockInfo> _infos;
        public StockInfo AcqireStockInfo(string symbol) {
            return _infos.Where(a => a.Symbol == symbol).OrderBy(a => a.Turnover).LastOrDefault();
        }
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
        public static TickerTable Create(DateTime date) => new TickerTable(date);

        public static IEnumerable<StockInfo> AcqireStockInfo(DateTime date,int start = 0,int end = 10) {
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
        /// <summary>
        /// 指定された日付のデータを取得する。
        /// </summary>
        /// <param name="date">日付</param>
        /// <returns>取得したデータ。取得できなかった場合、空のIEnumerableを返す。</returns>
        static IEnumerable<StockInfo> getStockData(DateTime date) {
            var ds = date.ToString("yyMMdd");
            string[] dwPath = { "mujinzo", "zips", ds + ".zip" };
            string[] acPath = { "mujinzo", "acv" , ds + ".csv" };

            var dfi = CacheManager.GetFileInfo(dwPath);
            var afi = CacheManager.GetFileInfo(acPath);

            if (afi.Exists) {
                return _readRecord(dfi,afi);
            } else if (dfi.Exists) {
                return _unZipper(dfi, afi);
            } else {
                return _download(date, dfi, afi);
            }
        }
        static WebClient wc = new WebClient() { Encoding = Encoding.Default };
        static IEnumerable<StockInfo> _download(DateTime dt,FileInfo dwnFi,FileInfo acFi) {
            string url = "http://souba-data.com/k_data/"
                + dt.ToString("yyyy") + "/"
                + dt.ToString("yy_MM") + "/T"
                + dt.ToString("yyMMdd") + ".zip";
            var fileName = dwnFi.FullName;
            try {
                wc.DownloadFile(url, fileName);
                dwnFi.Refresh();
                return _unZipper(dwnFi, acFi);
            } catch {
                return Enumerable.Empty<StockInfo>();
            }
        }
        static IEnumerable<StockInfo> _unZipper(FileInfo dwnFi, FileInfo acFi) {
            if (!dwnFi.Exists)
                return Enumerable.Empty<StockInfo>();
            using (ZipArchive zar = ZipFile.OpenRead(dwnFi.FullName)) {
                var tgt = zar.Entries
                    .Where(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                if (!tgt.Any())
                    return Enumerable.Empty<StockInfo>();
                var za = tgt.First();
                za.ExtractToFile(acFi.FullName);
                acFi.Refresh();
                return _readRecord(dwnFi,acFi);
            }
        }
        static IEnumerable<StockInfo> _readRecord(FileInfo dfi, FileInfo fi) {
            if (!fi.Exists)
                return Enumerable.Empty<StockInfo>();
            try {
                using (StreamReader str = new StreamReader(fi.FullName))
                using (var csv = new CsvReader(str)) {
                    csv.Configuration.HasHeaderRecord = false;
                    csv.Configuration.RegisterClassMap<MjzMap>();
                    var c = csv.GetRecords<StockInfo>().ToArray();
                    return c;
                }
            } catch {
                fi.Delete();
                if(dfi.Exists) dfi.Delete();
                return Enumerable.Empty<StockInfo>();
            }
        }

    }
}
