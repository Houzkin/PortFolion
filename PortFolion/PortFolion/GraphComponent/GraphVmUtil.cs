using Houzkin;
using Houzkin.Architecture;
using Houzkin.Tree;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Defaults;
using LiveCharts.Wpf;
using Livet;
using Livet.Commands;
using Livet.EventListeners.WeakEvents;
using Livet.Messaging;
using PortFolion.Core;
using PortFolion.IO;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace PortFolion.ViewModels {
	/// <summary>期間</summary>
	public enum Period {
		Weekly,
		Monthly,
		Quarterly,
		Yearly,
	}
	/// <summary>識別方法</summary>
	public enum DividePattern {
		Location,
		Tag,
		LocationAndTag,
	}
	/// <summary>残高に対するキャッシュフローの扱い方</summary>
	public enum BalanceCashFlow {
		Ignore,
		Flow,
		Stack,
	}

	public class SeriesValue : SeriesViewModel {
		public double Amount { get; set; }
		public double Rate { get; set; }
	}
	public class PlotValue {
		/// <summary>残高</summary>
		public double Amount { get; set; }
		/// <summary>外部キャッシュフロー</summary>
		public double Flow { get; set; }
		/// <summary>修正ディーツ法による変動比率(利回り)</summary>
		public double Dietz { get; set; } = 0;
		public DateTime Date { get; set; }
	}
	public class DateSpan {
		public DateSpan(DateTime date1, DateTime date2) {
			if (date1 <= date2) {
				Start = date1;
				End = date2;
			} else {
				Start = date2;
				End = date1;
			}
		}
		public DateTime Start { get; set; }
		public DateTime End { get; set; }
	}
	public static class Ext {
		public static IEnumerable<T> Dequeue<T>(this Queue<T> self, Func<T, bool> pred) {
			if (!self.Any()) return Enumerable.Empty<T>();
			var lst = new List<T>();
			while (self.Any() && pred(self.Peek())) {
				lst.Add(self.Dequeue());
			}
			return lst;
		}
		public static Dictionary<SeriesValue,Dictionary<DateTime,double>> ToStackGraphValues(this Dictionary<DateTime, CommonNode> src,
			CommonNode cur, Period period, int level, DividePattern divide) {

			var axs = GetTimeAxis(period, src.Keys);
			if (axs.IsEmpty() || cur == null)
				return new Dictionary<SeriesValue, Dictionary<DateTime, double>>();

			var curR = cur.MargeNodes(level, divide).ToArray();
			var initValue = curR.Select(a => new SeriesValue {
				Amount = 0,
				Title = a.Title,
			});

			var srcs = new Queue<KeyValuePair<DateTime, CommonNode>>(src);

			var gv = axs.Scan(initValue, (prv, ds) => {
				var s = srcs.Dequeue(a => ds.Start <= a.Key && a.Key <= ds.End);
				if (s.IsEmpty()) return prv;
				else return s.Last().Value.MargeNodes(level, divide);
			});

			var lst = new stackGenerator((cur.Root() as TotalRiskFundNode).CurrentDate, curR);
			axs.Zip(gv, (a, b) => Tuple.Create(a, b)).ForEach(a => {
				lst.Stack(a.Item1.End, a.Item2);
			});
			return lst.Quit();
		}
		private class stackGenerator {
			class eqal : IEqualityComparer<SeriesValue> {
				public bool Equals(SeriesValue x, SeriesValue y) {
					return x.Title == y.Title;
				}
				public int GetHashCode(SeriesValue obj) {
					return obj.Title.GetHashCode();
				}
			}
			SeriesValue emp = new SeriesValue() { Title = "", Amount = 0, };
			Dictionary<SeriesValue, Dictionary<DateTime, double>> dic;
			public stackGenerator(DateTime date, IEnumerable<SeriesValue> model) {
				dic = new Dictionary<SeriesValue, Dictionary<DateTime, double>>(new eqal());
				foreach(var m in model) {
					dic.Add(m, new Dictionary<DateTime, double>());
					dic[m].Add(date, m.Amount);
				}
				dic.Add(emp, new Dictionary<DateTime, double>());
				dic[emp].Add(date, 0);
			}
			public void Stack(DateTime date,IEnumerable<SeriesValue> items) {
				foreach(var itm in items) {
					if (dic.Keys.Any(a => a.Title == itm.Title)) {
						dic[itm].Add(date, itm.Amount);
					} else {
						double am;
						if(dic[emp].TryGetValue(date,out am)) 
							dic[emp][date] = am + itm.Amount;
						else 
							dic[emp].Add(date, itm.Amount);
					}
				}
				if (!dic[emp].ContainsKey(date)) dic[emp].Add(date, 0);
			}
			public Dictionary<SeriesValue, Dictionary<DateTime, double>> Quit() => dic;
		}
		
		public static IEnumerable<PlotValue> ToGraphValues(this Dictionary<DateTime, CommonNode> src, Period period) {
			if (src == null || !src.Any()) return Enumerable.Empty<PlotValue>();
			var ax = Ext.GetTimeAxis(period, src.Keys);
			var srcs = new Queue<KeyValuePair<DateTime, CommonNode>>(src);
			//var lst = new List<GraphValue>();

			var rslt = ax.Scan(new PlotValue(), (prev, ds) => {
				var gv = new PlotValue() { Date = ds.End };
				var tmp = srcs.Dequeue(a => ds.Start <= a.Key && a.Key <= ds.End);
				if (tmp.IsEmpty()) {
					gv.Amount = prev.Amount;
				} else {
					//期初時価総額
					var st = prev.Amount != 0 ? prev.Amount : tmp.First().Value.Amount - tmp.First().Value.InvestmentValue;
					//期末時価総額
					gv.Amount = tmp.Last().Value.Amount;
					//キャッシュフローを持つ要素
					var fl = tmp.Where(a => a.Value.InvestmentValue != 0).ToArray();
					//キャッシュフロー合計
					gv.Flow = fl.Sum(a => a.Value.InvestmentValue);

					var vc = gv.Amount - st - gv.Flow;

					var cf = fl.Aggregate(0D,
						(pr, cu) => pr + cu.Value.InvestmentValue * (ds.End - cu.Key).Days / (ds.End - ds.Start).Days);

					var rst = st + cf;
					if (rst != 0)
						gv.Dietz = vc / rst;
				}
				return gv;
			});
			return rslt.ToArray();
		}
		static IEnumerable<DateSpan> GetTimeAxis(Period period, IEnumerable<DateTime> dates) {
			DateTime start = dates.Min();
			DateTime end = dates.Max();
			switch (period) {
			case Period.Weekly:
				var wkax = Ext.weeklyAxis(start, end).ToArray();
				return wkax.Zip(wkax.Select(b => b.AddDays(-6)), (a, b) => new DateSpan(a, b));
			case Period.Monthly:
				var mtax = Ext.monthlyAxis(start, end).ToArray();
				return mtax.Zip(
					mtax.Select(a => new DateTime(a.Year, a.Month, 1)), (a, b) => new DateSpan(a, b));
			case Period.Quarterly:
				var qtax = Ext.quarterlyAxis(start, end).ToArray();
				return qtax.Zip(
					qtax.Select(a => new DateTime(a.Year, a.Month, 1).AddMonths(-2)), (a, b) => new DateSpan(a, b));
			case Period.Yearly:
				var yrax = Ext.yearlyAxis(start, end).ToArray();
				return yrax.Zip(yrax.Select(a => new DateTime(a.Year, 1, 1)), (a, b) => new DateSpan(a, b));
			default:
				var erd = dates.Take(1).Concat(dates.Select(a => a.AddDays(1)));
				return dates.Zip(erd, (a, b) => new DateSpan(a, b));
				//return Enumerable.Empty<DateSpan>();
			}
		}
		#region date axis static method
		/// <summary>週末日</summary>
		static IEnumerable<DateTime> weeklyAxis(DateTime start, DateTime end) {
			DateTime cur = start.DayOfWeek == DayOfWeek.Sunday ? start : start.AddDays(7 - (int)start.DayOfWeek);
			yield return cur;
			while(cur < end) {
				cur = cur.AddDays(7);
				yield return cur;
			}
		}
		/// <summary>月末日</summary>
		static IEnumerable<DateTime> monthlyAxis(DateTime start, DateTime end) {
			var c = EndOfMonth(start);
			yield return c;
			while (c < end) {
				c = NextEndOfMonth(c, 1);
				yield return c;
			}
		}
		/// <summary>四半期末日</summary>
		static IEnumerable<DateTime> quarterlyAxis(DateTime start, DateTime end) {
			int q = start.Month / 3;
			var c = EndOfMonth(new DateTime(start.Year, (q + 1) * 3, 1));
			yield return c;
			while (c < end) {
				c = NextEndOfMonth(c, 3);
				yield return c;
			}
		}
		/// <summary>年末日</summary>
		static IEnumerable<DateTime> yearlyAxis(DateTime start, DateTime end) {
			var c = new DateTime(start.Year, 12, 31);
			yield return c;
			while (c < end) {
				c = c.AddYears(1);
				yield return c;
			}
		}
		static int DaysInMonth(DateTime dt) {
			return DateTime.DaysInMonth(dt.Year, dt.Month);
		}
		static DateTime EndOfMonth(DateTime dt) {
			return new DateTime(dt.Year, dt.Month, DaysInMonth(dt));
		}
		static DateTime NextEndOfMonth(DateTime dt, int month) {
			var d = new DateTime(dt.Year, dt.Month, 1).AddMonths(month);
			return EndOfMonth(d);
		}
		#endregion
		/// <summary>現在のノードから指定した階層だけ下位のノードを返す。</summary>
		public static IEnumerable<CommonNode> TargetLevels(this CommonNode cur, int tgtLv) {
			var cd = cur.NodeIndex().CurrentDepth;
			var ch = cur.Height();
			var tg = Math.Min(ch, tgtLv) + cd;
			return cur.Levelorder()
				.SkipWhile(a => a.NodeIndex().CurrentDepth < tg)
				.TakeWhile(a => a.NodeIndex().CurrentDepth == tg);
		}
		/// <summary>指定した項目種別で括る。</summary>
		public static IEnumerable<SeriesValue> MargeNodes(this CommonNode current, int tgtLv, DividePattern div) {
			return current.TargetLevels(tgtLv).MargeNodes(div);
		}
		private class CashNodeCountedSeriesValue : SeriesValue {
			public int CashCount { get; set; }
		}
		public static IEnumerable<SeriesValue> MargeNodes(this IEnumerable<CommonNode> collection, DividePattern div) {
			if(div == DividePattern.Location || div == DividePattern.Tag){
				return _margeNodes(collection, div);
			}
			return _margeNodes(collection, DividePattern.Tag)
				.Select(t =>
					collection.ToLookup(a => a.Tag.TagName)
						.Where(a => a.Key == t.Title)
						.Select(a => _margeNodes(a, DividePattern.Location)))
				.SelectMany(b => b.SelectMany(c => c));
		}
		private static IEnumerable<SeriesValue> _margeNodes(IEnumerable<CommonNode> collection,DividePattern div){ 
			Func<CommonNode, string> DivFunc;
			switch (div) {
			//case DividePattern.Location:
			//	DivFunc = c => c.Name;
			//	break;
			case DividePattern.Tag:
				DivFunc = c => c.Tag.TagName;
				break;
			default:
				DivFunc = c => c.Name;
				break;
			}
			var ttl = (double)(collection.Sum(a => (a.Amount)));
			if (ttl == 0) return Enumerable.Empty<SeriesValue>();
			return collection
				.ToLookup(DivFunc)
				.Select(a => {
					var tv = new CashNodeCountedSeriesValue();
					tv.Title = a.Key;
					tv.Amount = a.Sum(b => b.Amount);
					tv.Rate = ((double)tv.Amount / (double)ttl);
					tv.CashCount = a.Count(b => b.GetNodeType() == NodeType.Cash);
					return tv;
				})
				.GroupBy(a => a.CashCount)
				.OrderBy(a => a.Key)
				.SelectMany(a => a.OrderByDescending(b => b.Amount));
		}

		static Random rdm = new Random();
		static int rdmIdx = -1;
		public static void ResetColorIndex() {
			rdmIdx = rdm.Next(BrushColors().Count);
		}
		public static List<Color> BrushOrder() {
			if (rdmIdx < 0) ResetColorIndex();
			return BrushColors().Repeat().Skip(rdmIdx).Take(BrushColors().Count).ToList();
		}
		static List<Color> pieColors;
		public static List<Color> PieBrushColors(int length) {
			if(pieColors == null) {
				pieColors = new List<Color>() {
					Color.FromRgb(17,140,18),
					Color.FromRgb(214,54,96),
					Color.FromRgb(59,67,255),
					Color.FromRgb(167,68,227),
					Color.FromRgb(237,103,30),

					Color.FromRgb(98,129,19),
					Color.FromRgb(57,63,158),
					Color.FromRgb(248,63,27),
					Color.FromRgb(130,16,142),
					Color.FromRgb(21,157,184),

					Color.FromRgb(175,176,0),
					Color.FromRgb(221,39,124),
					Color.FromRgb(132,79,175),
					Color.FromRgb(7,190,48),
					Color.FromRgb(216,105,26),

					Color.FromRgb(33,145,242),
					Color.FromRgb(225,116,223),
					Color.FromRgb(95,44,159),
					Color.FromRgb(170,119,10),
					Color.FromRgb(69,202,212),

					Color.FromRgb(199,27,76),
					Color.FromRgb(212,200,11),
					Color.FromRgb(11,186,94),
					Color.FromRgb(97,37,241),
					Color.FromRgb(255,81,5),
				};
			}
			var c = pieColors.Repeat().Take(length).ToList();
			if(length % pieColors.Count == 1 && 1 < length) {
				c.RemoveAt(length - 1);
				c.Add(pieColors[3]);
			}
            return c;
		}
		static List<Color> colors;
		public static List<Color> BrushColors() {
			//object obj = ColorConverter.ConvertFromString("#51000000");
			//SolidColorBrush ret = new SolidColorBrush((System.Windows.Media.Color)obj);
			return colors = colors ?? new List<Color>() {
				Color.FromRgb(22,207,53),
				Color.FromRgb(255,72,185),
				Color.FromRgb(91,108,247),
				Color.FromRgb(255,156,64),
				Color.FromRgb(187,87,248),

				Color.FromRgb(28,247,194),
				Color.FromRgb(242,212,24),
				Color.FromRgb(212,49,149),
				Color.FromRgb(144,198,43),
				Color.FromRgb(117,92,246),

				Color.FromRgb(246,92,100),
				Color.FromRgb(211,138,62),
				Color.FromRgb(190,99,243),
				Color.FromRgb(197,228,23),
				Color.FromRgb(26,199,146),

				Color.FromRgb(248,59,13),
				Color.FromRgb(88,112,255),
				Color.FromRgb(253,88,254),
				Color.FromRgb(220,150,20),
				Color.FromRgb(20,157,53),

				Color.FromRgb(142,55,151),
				Color.FromRgb(235,59,70),
				Color.FromRgb(248,157,13),
				Color.FromRgb(40,185,199),
				
			};
		}
	}
}
