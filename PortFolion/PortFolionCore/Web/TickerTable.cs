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
using System.Text.RegularExpressions;

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
		/// <summary>マップ</summary>
		class MjzMap : ClassMap<StockInfo> {
			public MjzMap() {
				//日付　銘柄コード　市場コード　銘柄コード＋銘柄名　始値　高値　安値　終値　出来高　市場名
				Map(m => m.Date).Index(0).TypeConverterOption.Format("yyyy/MM/dd");
				Map(m => m.Symbol).Index(1).Default("0000");
				Map(m => m.Name).Index(3).TypeConverter<TickerConverter>().Default("unknown");
				Map(m => m.Open).Index(4).Default(0);
				Map(m => m.High).Index(5).Default(0);
				Map(m => m.Low).Index(6).Default(0);
				Map(m => m.Close).Index(7).Default(0);
				Map(m => m.Turnover).Index(8).Default(0);
				Map(m => m.Market).Index(9).Default("");
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
					return "unknown";
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

		public static IEnumerable<StockInfo> AcqireStockInfo(DateTime date, int start = 0, int end = 10) {
			for (int i = -1 * start; i > -1 * end; i--) {
				date = DayOfWeekSkip(date.AddDays(i));
				var stockInfos = getStockData(date);
				if (stockInfos.Any()) {
					foreach (var si in stockInfos) {
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
			string[] acPath = { "mujinzo", "acv", ds + ".csv" };

			var dfi = CacheManager.GetFileInfo(dwPath);
			var afi = CacheManager.GetFileInfo(acPath);

			if (afi.Exists) {
				return _readRecord(dfi, afi);
			}
			else if (dfi.Exists) {
				return _unZipper(dfi, afi);
			}
			else {
				return _download(date, dfi, afi);
			}
		}
		static private readonly WebClient wc = new WebClient() { Encoding = Encoding.Default };
		static IEnumerable<StockInfo> _download(DateTime dt, FileInfo dwnFi, FileInfo acFi) {
			//無尽蔵URL変更に合わせた調節
			string baseUrl = dt.Year < 2019 ? "http://souba-data.com/k_data/" : "http://mujinzou.com/k_data/";
			string url = baseUrl // "http://mujinzou.com/k_data/" //"http://souba-data.com/k_data/"
				+ dt.ToString("yyyy") + "/"
				+ dt.ToString("yy_MM") + "/T"
				+ dt.ToString("yyMMdd") + ".zip";
			var fileName = dwnFi.FullName;
			try {
				wc.DownloadFile(url, fileName);
				dwnFi.Refresh();
				return _unZipper(dwnFi, acFi);
			}
			catch {
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
				return _readRecord(dwnFi, acFi);
			}
		}
		static IEnumerable<StockInfo> _readRecord(FileInfo dfi, FileInfo fi) {
			if (!fi.Exists)
				return Enumerable.Empty<StockInfo>();
			try {
				var ecd = Encoding.GetEncoding("shift_jis");
				using (StreamReader str = new StreamReader(fi.FullName, ecd))
				using (var csv = new CsvReader(str, System.Globalization.CultureInfo.InvariantCulture)) {
					csv.Configuration.HasHeaderRecord = false;
					csv.Configuration.RegisterClassMap<MjzMap>();
					var c = csv.GetRecords<StockInfo>().ToArray();
					return c;
				}
			}
			catch {
				fi.Delete();
				if (dfi.Exists) dfi.Delete();
				return Enumerable.Empty<StockInfo>();
			}
		}

	}
	public class Kabuoji3 {

		class KabuojiMap : ClassMap<AdjStockInfo> {
			public KabuojiMap() {
				Map(x => x.Date).Index(0).TypeConverterOption.Format("yyyy-MM-dd");
				Map(x => x.Open).Index(1).Default(0);
				Map(x => x.High).Index(2).Default(0);
				Map(x => x.Low).Index(3).Default(0);
				Map(x => x.Close).Index(4).Default(0);
				Map(x => x.Turnover).Index(5).Default(0);
				Map(x => x.AdjClose).Index(6).Default(0);
			}
		}
		private static List<string> getCodeAndSymbol(string text) {
			text = Regex.Replace(text, @"\(.+\)", "");
			var s = Regex.Replace(text, @"（|）", " ").Split(' '); 
			return new List<string>(s);
		}

		private static IEnumerable<AdjStockInfo> getInfos(int year, string symbol) {
			string param = string.Format("code={0}&year={1}&csv=", symbol, year.ToString());
			byte[] byt = Encoding.ASCII.GetBytes(param);
			HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://kabuoji3.com/stock/file.php");
			req.ContentType = "application/x-www-form-urlencoded";
			req.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/87.0.4280.88 Safari/537.36 Edg/87.0.664.66";
			req.Method = "POST";
			req.Referer = "https://kabuoji3.com/stock/download.php";
			req.ContentLength = byt.Length;
			using (var reqstrm = req.GetRequestStream()) {
				reqstrm.Write(byt, 0, byt.Length);
			}
			try {
				var res = (HttpWebResponse)req.GetResponse();
				using (var stream = res.GetResponseStream())
				using (var strr = new StreamReader(stream, Encoding.GetEncoding("shift_jis")))
				using (var csv = new CsvReader(strr, System.Globalization.CultureInfo.InvariantCulture)) {
					var elss = getCodeAndSymbol(strr.ReadLine());
					//var elsss = strr.ReadLine();//new List<string>() { "symbol", "market", "name", };
					csv.Configuration.HasHeaderRecord = true;
					csv.Configuration.RegisterClassMap<KabuojiMap>();
					var lst = csv.GetRecords<AdjStockInfo>().ToList();
					if (3 <= elss.Count) {
						foreach (var asi in lst) {
							asi.Symbol = elss[0];
							asi.Market = elss[1];
							asi.Name = elss[2];
						}
					}
					return lst;
				}
			}
			catch (Exception e) {
				return Enumerable.Empty<AdjStockInfo>();
				//throw e;
			}
		}
		private static IEnumerable<AdjStockInfo> getInfos(int sinceYear, int untilYear, string symbol) {
			var lst = Enumerable.Empty<AdjStockInfo>();
			for (int i = sinceYear; i <= untilYear; i++) {
				lst = lst.Concat(getInfos(i, symbol));
			}
			return lst;
		}
		public static IEnumerable<AdjStockInfo> GetInfos(DateTime date, IEnumerable<string> symbols) {
			var lst = new List<AdjStockInfo>();
			foreach (var symbol in symbols) {
				var v = getInfos(date.Year, date.Year, symbol).Where(b => b.Date <= date).OrderByDescending(x => x.Date).FirstOrDefault() ??
					getInfos(date.Year - 1, date.Year - 1, symbol).OrderByDescending(x => x.Date).FirstOrDefault();
				if (v != null) lst.Add(v);
			}
			return lst;
		}
		public static AdjStockInfo GetInfo(DateTime date, string symbol) {
			var s = new List<string>() { symbol, };
			return GetInfos(date, s).FirstOrDefault();
		}
		public static IEnumerable<AdjStockInfo> GetInfos(DateTime since, DateTime until, string symbol) {
			return getInfos(since.Year, until.Year, symbol).Where(a => since <= a.Date && a.Date <= until);
		}

	}
}
