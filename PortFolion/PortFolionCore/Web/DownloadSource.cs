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
        class MjzMap : ClassMap<StockInfo> {
            public MjzMap() {
                Map(m => m.Symbol).Index(1).Default(0);//
                Map(m => m.Name).Index(3).TypeConverter<TickerConverter>().Default("");//
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
                return base.ConvertFromString(text, row, memberMapData);
            }
            //public override bool CanConvertFrom(Type type) {
            //    return type == typeof(string);
            //}
            //public override object ConvertFromString(TypeConverterOptions options, string text) {
            //    var s = text.Split(' ').FirstOrDefault();
            //    var numberStyle = options.NumberStyle ?? System.Globalization.NumberStyles.Integer;
            //    int i;
            //    if(int.TryParse(s,numberStyle,options.CultureInfo,out i)) {
            //        return i.ToString();
            //    }
            //    return "unknown";
            //}
        }
        class TickerConverter : DefaultTypeConverter {
            public override object ConvertFromString(string text, IReaderRow row, MemberMapData memberMapData) {
                return base.ConvertFromString(text, row, memberMapData);
            }
            //public override bool CanConvertFrom(Type type) {
            //    return type == typeof(string);
            //}
            //public override object ConvertFromString(TypeConverterOptions options, string text) {
            //    var s = text.Split(' ').Skip(1);
            //    if (s.Any())
            //        return s.First();
            //    else
            //        return "unknown";
            //}
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
            var ds = date.ToString("yyMMdd");
            string[] dwPath = { "mujinzo", "zips", ds + ".zip" };
            string[] acPath = { "mujinzo", "acv" , ds + ".csv" };

            var fi = CacheManager.GetFileInfo(dwPath);
            if (!isDownloadable(fi))
                return Enumerable.Empty<StockInfo>();
            var rwv = _download(date, fi);
            if (!rwv.Result)
                return Enumerable.Empty<StockInfo>();
            rwv.Value.Refresh();

            rwv = _unZipper(CacheManager.GetFileInfo(acPath).FullName, rwv.Value);
            if (!rwv.Result)
                return Enumerable.Empty<StockInfo>();
            rwv.Value.Refresh();

            var r = _toArray(rwv.Value);
            return r;
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
        static ResultWithValue<FileInfo> _unZipper(string acvPath, FileInfo dwnFi) {
            if (dwnFi == null)
                return new ResultWithValue<FileInfo>();
            using (ZipArchive zar = ZipFile.OpenRead(dwnFi.FullName)) {
                var tgt = zar.Entries
                    .Where(e => e.FullName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase));
                if (!tgt.Any())
                    return new ResultWithValue<FileInfo>();
                var za = tgt.First();
                za.ExtractToFile(acvPath);
                return new ResultWithValue<FileInfo>(new FileInfo(acvPath));
            }
        }
        static IEnumerable<StockInfo> _toArray(FileInfo fi) {
            //try {
                using (StreamReader str = new StreamReader(fi.FullName))
                using (var csv = new CsvReader(str)) {
                    //csv.Configuration.HasHeaderRecord = false;
                //csv.Configuration.IgnoreReadingExceptions = true;

                csv.Configuration.RegisterClassMap<MjzMap>();
                    //csv.Configuration.WillThrowOnMissingField = false;
                    var c = csv.GetRecords<StockInfo>().ToArray();
                    return c;
                }
            //} catch {
            //    fi.Delete();
            //    return Enumerable.Empty<StockInfo>();
            //}
        }
        
    }
}
