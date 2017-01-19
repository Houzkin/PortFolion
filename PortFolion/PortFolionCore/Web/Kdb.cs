using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Houzkin;
using System.Net;
using PortFolion.IO;

namespace PortFolion.Web {
	public class StockInfo {

	}
	public enum KdbData { indices,stocks, }
	public class KdbDataClient {
		
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
		public static IEnumerable<StockInfo> AcqireInfo(DateTime date,KdbData type, int start = 0,int end = 10) {
			for(int i= -1*start; i > -1*end; i--) {
				date = DayOfWeekSkip(date.AddDays(i));
				var infoStr = getData(date, type);
			}
			return Enumerable.Empty<StockInfo>();
		}
		static string getData(DateTime date, KdbData type) {
			string[] path = { "kdb", type.ToString(), date.ToString("yyyy-MM-dd")+".csv" };
			string data;
			if (!CacheManager.GetFileInfo(path).Exists) {
				data = _download(date, type);
				CacheManager.SaveCache(data, path);
			}
			data = CacheManager.ReadCache(path);
			return data;
		}
		static WebClient wc = new WebClient() { Encoding = Encoding.Default };
		static string _download(DateTime dt, KdbData type) {
			string url = "http://k-db.com/" + type.ToString() + "/" + dt.ToString("yyyy-MM-dd") + "?download=csv";
			return wc.DownloadString(url);
		}

	}
}
