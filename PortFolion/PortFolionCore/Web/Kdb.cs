using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin;
using System.Net;
using PortFolion.IO;
using PortFolion.Core;
using System.IO;

namespace PortFolion.Web {
	
	//public enum KdbData { indices,stocks, }
	//public static class KdbDataClient {

	//	#region CsvMapping
	//	class KdbMap : CsvClassMap<StockInfo> {
	//		public KdbMap() {
	//			Map(m => m.Symbol).Name("コード").TypeConverter<SymbolConverter>().Default("unknown");
	//			Map(m => m.Name).Name("銘柄名").Default("");
	//			Map(m => m.Market).Name("市場").Default("");
	//			Map(m => m.Open).Name("始値").Default(0);
	//			Map(m => m.High).Name("高値").Default(0);
	//			Map(m => m.Low).Name("安値").Default(0);
	//			Map(m => m.Close).Name("終値").Default(0);
	//			Map(m => m.Turnover).Name("出来高").Default(0);
	//			Map(m => m.TradingValue).Name("売買代金").Default(0);
	//		}
	//	}
	//	class SymbolConverter : DefaultTypeConverter {
	//		public override bool CanConvertFrom(Type type) {
	//			return type == typeof(string);
	//		}
	//		public override object ConvertFromString(TypeConverterOptions options, string text) {
	//			var s = text.Split('-').FirstOrDefault();
	//			var numberStyle = options.NumberStyle ?? System.Globalization.NumberStyles.Integer;
	//			int i;
	//			if (int.TryParse(s, numberStyle, options.CultureInfo, out i)) {
	//				return i.ToString();
	//			}
	//			return "unknown";
	//		}
	//	}
	//	#endregion

	//	static DateTime DayOfWeekSkip(DateTime date) {
	//		switch (date.DayOfWeek) {
	//		case DayOfWeek.Sunday:
	//			return date.AddDays(-2);
	//		case DayOfWeek.Saturday:
	//			return date.AddDays(-1);
	//		default:
	//			return date;
	//		}
	//	}
	//	public static IEnumerable<StockInfo> AcqireStockInfo(DateTime date, int start = 0,int end = 10) {
	//		for(int i= -1*start; i > -1*end; i--) {
	//			date = DayOfWeekSkip(date.AddDays(i));
	//			var stockInfos = getStockData(date);
	//			if (stockInfos.Any()) {
	//				foreach(var si in stockInfos) {
	//					si.Date = date.Date;
	//					yield return si;
	//				}
	//				yield break;
	//			}
	//		}
	//		yield break;
	//	}
	//	static bool isDownloadable(FileInfo fi,DateTime date) {
	//		return (!fi.Exists);
	//	}
	//	static IEnumerable<StockInfo> getStockData(DateTime date) {
	//		var type = KdbData.stocks;
	//		string[] path = { "kdb", type.ToString(), date.ToString("yyyy-MM-dd")+".csv" };
	//		var fi = CacheManager.GetFileInfo(path);
	//		if (isDownloadable(fi, date)) { 
	//			CacheManager.SaveCache(_download(date,type), path);
	//			fi.Refresh();
	//		}
	//		try {
	//			using (StreamReader str = new StreamReader(fi.FullName))
	//			using (var csv = new CsvReader(str)) {
	//				csv.Configuration.RegisterClassMap<KdbMap>();

	//				csv.Configuration.WillThrowOnMissingField = false;
	//				return csv.GetRecords<StockInfo>().ToArray();
	//			}
	//		} catch {
	//			fi.Delete();
	//			return Enumerable.Empty<StockInfo>();
	//		}
	//	}
	//	static WebClient wc = new WebClient() { Encoding = Encoding.Default };
	//	static string _download(DateTime dt, KdbData type) {
	//		string url = "http://k-db.com/" + type.ToString() + "/" + dt.ToString("yyyy-MM-dd") + "?download=csv";
	//		return wc.DownloadString(url);
	//	}

	//}
}
